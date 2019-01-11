using Amazon.S3;
using Amazon.S3.Model;
using StorageManagementKit.Core.AWS;
using StorageManagementKit.Core.Copying;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Transforms;
using System;
using System.IO;
using System.Linq;

namespace StorageManagementKit.Core.Restoring
{
    public class S3ObjectRestore : IObjectRestoring
    {
        #region Properties
        private string _bucketName;
        private string _keyFile;
        private byte[] _crypto_key;
        private byte[] _crypto_iv;
        private ILogging _logger;
        private AmazonS3Client _client;
        #endregion

        #region Constructor
        /// <param name="bucketName">S3 bucket name</param>
        /// <param name="keyFile">Api key for S3</param>
        /// <param name="crypto_key">3-DES key</param>
        /// <param name="crypto_iv">3-DES vector</param>
        public S3ObjectRestore(string bucketName, string keyFile, byte[] crypto_key, byte[] crypto_iv, ILogging logger)
        {
            _bucketName = bucketName ?? throw new ArgumentNullException("bucketName");
            _keyFile = keyFile ?? throw new ArgumentNullException("keyFile");
            _crypto_key = crypto_key ?? throw new ArgumentNullException("key");
            _crypto_iv = crypto_iv ?? throw new ArgumentNullException("iv");
            _logger = logger ?? throw new ArgumentNullException("logger");

            S3Credentials credentials = S3Credentials.LoadKey(keyFile);
            _client = new AmazonS3Client(credentials.AccessKeyId, credentials.SecretAccessKey, credentials.AwsRegion);
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Returns the list of restore points for a specific object
        /// </summary>
        public ObjectVersion[] GetVersions(string filename)
        {
            try
            {
                string fmtObject = $"{filename}.encrypted";

                ListVersionsRequest request = new ListVersionsRequest()
                {
                    BucketName = _bucketName,
                    MaxKeys = int.MaxValue,
                    Prefix = filename
                };

                ListVersionsResponse response = _client.ListVersionsAsync(request).Result;

                // Be sure the object exists
                if ((response.Versions == null) || (response.Versions.Count() == 0))
                {
                    _logger.WriteLog(ErrorCodes.S3ObjectRestore_ObjectNotFound,
                        string.Format(ErrorResources.S3ObjectRestore_ObjectNotFound, filename),
                        Severity.Error, VerboseLevel.User);
                    return null;
                }

                var list = response.Versions;

                // Build a list of objects found
                return list.Select(item =>
                    new ObjectVersion()
                    {
                        Name = item.Key,
                        TimeCreated = item.LastModified,
                        StorageClass = item.StorageClass,
                        Size = item.Size,
                        VersionId = item.VersionId
                    }).ToArray();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ErrorCodes.S3ObjectRestore_GetVersionsException,
                    ErrorResources.S3ObjectRestore_GetVersionsException + Environment.NewLine + ex.Message,
                    Severity.Error, VerboseLevel.User);
                return null;
            }
        }

        /// <summary>
        /// Restore the object for a specific date time
        /// </summary>
        public bool Restore(ObjectVersion version, ref string destination)
        {
            try
            {
                GetObjectRequest request = new GetObjectRequest()
                {
                    BucketName = _bucketName,
                    Key = version.Name,
                    VersionId = (string)version.VersionId
                };

                using (GetObjectResponse response = _client.GetObjectAsync(request).Result)
                {
                    response.WriteObjectProgressEvent += ResponseWriteObjectProgressEvent;

                    // Extracts the metadata values
                    string originalMD5 = null, metadataMD5 = null, metadataEncrypted = null;
                    if (!ExtractMetadata(version.Name, response.Metadata, out originalMD5, out metadataMD5, out metadataEncrypted))
                        return false;

                    FileObject fo = new FileObject()
                    {
                        Metadata = new FileMetadata() { OriginalMD5 = originalMD5 },
                        DataContent = null,
                        IsSecured = true,
                        MetadataMD5 = metadataMD5,
                        MetadataContent = metadataEncrypted
                    };

                    // Extracts the file content
                    using (MemoryStream stm = new MemoryStream())
                    {
                        response.ResponseStream.CopyTo(stm);
                        stm.Position = 0;
                        fo.DataContent = stm.ToArray();
                    }

                    Helpers.WriteProgress("Decrypting...");
                    fo = new UnsecureTransform(_crypto_key, _crypto_iv, _logger).Process(fo);

                    Helpers.WriteProgress("Saving...");
                    Helpers.WriteProgress("");

                    // Force a destination name if not given by the user
                    if (string.IsNullOrEmpty(destination))
                    {
                        string destName = version.Name.Replace(Constants.EncryptedExt, "");
                        destination = Path.GetFileNameWithoutExtension(destName);
                        destination = $"{destination}.{version.VersionId}.restore{Path.GetExtension(destName)}";
                    }

                    File.WriteAllBytes(destination, fo.DataContent);
                    File.SetAttributes(destination, fo.Metadata.Attributes);
                    File.SetLastWriteTime(destination, fo.Metadata.LastWriteTime);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ErrorCodes.S3ObjectRestore_RestoreObjectException,
                    ErrorResources.S3ObjectRestore_RestoreObjectException + Environment.NewLine + ex.Message,
                    Severity.Error, VerboseLevel.User);
                return false;
            }
        }

        /// <summary>
        /// Downloading progression state reported by S3 SDK
        /// </summary>
        private void ResponseWriteObjectProgressEvent(object sender, WriteObjectProgressArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.CursorLeft = 0;
            Helpers.WriteProgress(e.PercentDone);
        }

        /// <summary>
        /// Extracts the metadata values from the collection.
        /// </summary>
        private bool ExtractMetadata(string objectKey, MetadataCollection metadata, out string originalMD5, out string metadataMD5, out string metadataEncrypted)
        {
            originalMD5 = null;
            metadataMD5 = null;
            metadataEncrypted = null;

            string nameOriginalMD5 = $"x-amz-meta-{Constants.OriginalMD5Key}".ToLower();
            string nameMetaMD5 = $"x-amz-meta-{Constants.MetadataMD5Key}".ToLower();
            string nameMetaEncrypted = $"x-amz-meta-{Constants.MetadataEncryptedKey}".ToLower();

            foreach (var key in metadata.Keys)
            {
                if (key == nameOriginalMD5)
                    originalMD5 = metadata[key];

                if (key == nameMetaMD5)
                    metadataMD5 = metadata[key];

                if (key == nameMetaEncrypted)
                    metadataEncrypted = metadata[key];
            }

            if ((originalMD5 == null) || (metadataMD5 == null) || (metadataEncrypted == null))
            {
                _logger.WriteLog(ErrorCodes.S3ObjectRestore_MissingMetadata,
                    string.Format(ErrorResources.S3ObjectRestore_MissingMetadata, objectKey),
                    Severity.Error, VerboseLevel.User);
                return false;
            }

            return true;
        }
        #endregion
    }
}
