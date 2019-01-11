using Amazon.S3;
using Amazon.S3.Model;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using StorageManagementKit.Core.AWS;
using StorageManagementKit.Core.Copying;
using StorageManagementKit.Core.Diagnostics;
using System;
using System.Linq;

namespace StorageManagementKit.Core.Restoring
{
    public class S3Restore : IProgress<IDownloadProgress>, IRestoring
    {
        #region Properties
        private long _fileSize;
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
        public S3Restore(string bucketName, string keyFile, byte[] crypto_key, byte[] crypto_iv, ILogging logger)
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
                    _logger.WriteLog(ErrorCodes.S3Restore_ObjectNotFound,
                        string.Format(ErrorResources.S3Restore_ObjectNotFound, filename),
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
                        Generation = item.VersionId
                    }).ToArray();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ErrorCodes.S3Restore_GetVersionsException,
                    ErrorResources.S3Restore_GetVersionsException + Environment.NewLine + ex.Message,
                    Severity.Error, VerboseLevel.User);
                return null;
            }
        }

        /// <summary>
        /// Restore the object for a specific date time
        /// </summary>
        public bool Restore(ObjectVersion version, ref string destination)
        {
            //try
            //{
            //    using (StorageClient _client = StorageClient.Create(GoogleCredential.FromFile(_apiKey)))
            //    {
            //        var gen = new GetObjectOptions() { Generation = (long?)version.Generation };

            //        var obj = _client.GetObject(_bucketName, version.Name, gen);

            //        string originalMD5, metadataMD5, metadataEncrypted;

            //        if (!obj.Metadata.TryGetValue(Constants.OriginalMD5Key, out originalMD5) ||
            //            !obj.Metadata.TryGetValue(Constants.MetadataMD5Key, out metadataMD5) ||
            //            !obj.Metadata.TryGetValue(Constants.MetadataEncryptedKey, out metadataEncrypted))
            //        {
            //            _logger.WriteLog(ErrorCodes.GcsRestore_MissingMetadata,
            //                string.Format(ErrorResources.GcsRestore_MissingMetadata, obj.Name),
            //                Severity.Error, VerboseLevel.User);
            //            return false;
            //        }

            //        FileObject fo = new FileObject()
            //        {
            //            Metadata = new FileMetadata() { OriginalMD5 = originalMD5 },
            //            DataContent = null,
            //            IsSecured = true,
            //            MetadataMD5 = metadataMD5,
            //            MetadataContent = metadataEncrypted
            //        };

            //        DownloadObject(_client, obj, fo, version);

            //        Helpers.WriteProgress("Decrypting...");
            //        fo = new UnsecureTransform(_crypto_key, _crypto_iv, _logger).Process(fo);

            //        Helpers.WriteProgress("Saving...");
            //        Helpers.WriteProgress("");

            //        // Force a destination name if not given by the user
            //        if (string.IsNullOrEmpty(destination))
            //        {
            //            string destName = version.Name.Replace(Constants.EncryptedExt, "");
            //            destination = Path.GetFileNameWithoutExtension(destName);
            //            destination = $"{destination}.{version.Generation}.restore{Path.GetExtension(destName)}";
            //        }

            //        File.WriteAllBytes(destination, fo.DataContent);
            //        File.SetAttributes(destination, fo.Metadata.Attributes);
            //        File.SetLastWriteTime(destination, fo.Metadata.LastWriteTime);

            //        return true;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.WriteLog(ErrorCodes.GcsRestore_RestoreObjectException,
            //        ErrorResources.GcsRestore_RestoreObjectException + Environment.NewLine + ex.Message,
            //        Severity.Error, VerboseLevel.User);
            //    return false;
            //}
            throw new NotSupportedException();
        }

        /// <summary>
        /// Downloading progression state reported by GCS SDK
        /// </summary>
        void IProgress<IDownloadProgress>.Report(IDownloadProgress value)
        {
            int percent = (int)(value.BytesDownloaded * 100 / _fileSize);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.CursorLeft = 0;
            Helpers.WriteProgress(percent);
        }

        /// <summary>
        /// Download a file object from S3
        /// </summary>
        private void DownloadObject(StorageClient client, Google.Apis.Storage.v1.Data.Object gcsObject,
            FileObject fileObject, ObjectVersion version)
        {
           
        }
        #endregion
    }
}
