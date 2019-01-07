using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using StorageManagementKit.Core.Crypto;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Repositories;
using StorageManagementKit.Core.Transforms;
using System;
using System.IO;
using System.Linq;

namespace StorageManagementKit.Core.Restoring
{
    public class GcsRestore : IProgress<IDownloadProgress>
    {
        #region Properties
        private long _fileSize;
        private string _bucketName;
        private string _apiKey;
        private byte[] _crypto_key;
        private byte[] _crypto_iv;
        private ILogging _logger;
        #endregion

        #region Constructor
        /// <param name="bucketName">GCS bucket name</param>
        /// <param name="apiKey">OAuth GCS key</param>
        /// <param name="crypto_key">3-DES key</param>
        /// <param name="crypto_iv">3-DES vector</param>
        public GcsRestore(string bucketName, string apiKey, string keyFile, ILogging logger, string destination)
        {
            _bucketName = bucketName ?? throw new ArgumentNullException("bucketName");
            _apiKey = apiKey ?? throw new ArgumentNullException("apiKey");
            keyFile = keyFile ?? throw new ArgumentNullException("keyFile");
            _logger = logger ?? throw new ArgumentNullException("logger");

            // Loads the 3-DES key to decrypt the file from GCS
            if (!TripleDES.LoadKeyFile(keyFile, out _crypto_key, out _crypto_iv, logger))
                throw new SmkException(ErrorResources.TransformFactory_InstanciationFailed);
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
                using (StorageClient _client = StorageClient.Create(GoogleCredential.FromFile(_apiKey)))
                {
                    ListObjectsOptions options = new ListObjectsOptions();
                    options.Versions = true;

                    string fmtObject = $"{filename}.encrypted";
                    var list = _client.ListObjects(_bucketName, fmtObject, options).ToList();

                    // Be sure the object exists
                    if ((list == null) || (list.Count() == 0))
                    {
                        _logger.WriteLog(ErrorCodes.GcsRestore_ObjectNotFound,
                            string.Format(ErrorResources.GcsRestore_ObjectNotFound, filename),
                            Severity.Error, VerboseLevel.User);
                        return null;
                    }

                    // Build a list of objects found
                    return list.Select(item =>
                        new ObjectVersion()
                        {
                            Name = item.Name,
                            TimeCreated = item.TimeCreated.GetValueOrDefault(),
                            StorageClass = item.StorageClass,
                            Size = (long)item.Size.GetValueOrDefault(),
                            Generation = item.Generation
                        }).ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ErrorCodes.GcsRestore_GetVersionsException,
                    ErrorResources.GcsRestore_GetVersionsException + Environment.NewLine + ex.Message,
                    Severity.Error, VerboseLevel.User);
                return null;
            }
        }

        /// <summary>
        /// Restore the object for a specific date time
        /// </summary>
        public bool RestoreObject(ObjectVersion version, ref string destination)
        {
            try
            {
                using (StorageClient _client = StorageClient.Create(GoogleCredential.FromFile(_apiKey)))
                {
                    var gen = new GetObjectOptions() { Generation = version.Generation };

                    var obj = _client.GetObject(_bucketName, version.Name, gen);

                    string originalMD5, metadataMD5, metadataEncrypted;

                    if (!obj.Metadata.TryGetValue(Constants.OriginalMD5Key, out originalMD5) ||
                        !obj.Metadata.TryGetValue(Constants.MetadataMD5Key, out metadataMD5) ||
                        !obj.Metadata.TryGetValue(Constants.MetadataEncryptedKey, out metadataEncrypted))
                    {
                        _logger.WriteLog(ErrorCodes.GcsRestore_MissingMetadata,
                            string.Format(ErrorResources.GcsRestore_MissingMetadata, obj.Name),
                            Severity.Error, VerboseLevel.User);
                        return false;
                    }

                    FileObject fo = new FileObject()
                    {
                        Metadata = new FileMetadata() { OriginalMD5 = originalMD5 },
                        DataContent = null,
                        IsSecured = true,
                        MetadataMD5 = metadataMD5,
                        MetadataContent = metadataEncrypted
                    };

                    DownloadObject(_client, obj, fo, version);

                    Helpers.WriteProgress("Decrypting...");
                    fo = new UnsecureTransform(_crypto_key, _crypto_iv, _logger).Process(fo);

                    Helpers.WriteProgress("Saving...");
                    Helpers.WriteProgress("");

                    // Force a destination name if not given by the user
                    if (string.IsNullOrEmpty(destination))
                    {
                        string destName = version.Name.Replace(Constants.EncryptedExt, "");
                        destination = Path.GetFileNameWithoutExtension(destName);
                        destination = $"{destination}.{version.Generation}.restore{Path.GetExtension(destName)}";
                    }

                    File.WriteAllBytes(destination, fo.DataContent);
                    File.SetAttributes(destination, fo.Metadata.Attributes);
                    File.SetLastWriteTime(destination, fo.Metadata.LastWriteTime);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ErrorCodes.GcsRestore_RestoreObjectException,
                    ErrorResources.GcsRestore_RestoreObjectException + Environment.NewLine + ex.Message,
                    Severity.Error, VerboseLevel.User);
                return false;
            }
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
        /// Download a file object from GCS
        /// </summary>
        private void DownloadObject(StorageClient client, Google.Apis.Storage.v1.Data.Object gcsObject,
            FileObject fileObject, ObjectVersion version)
        {
            try
            {
                using (MemoryStream memStm = new MemoryStream())
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Helpers.WriteProgress(0);

                    _fileSize = (long)gcsObject.Size.GetValueOrDefault();
                    client.DownloadObject(gcsObject, memStm, new DownloadObjectOptions() { Generation = version.Generation }, this);

                    memStm.Position = 0;
                    fileObject.DataContent = memStm.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new SmkException(string.Format(ErrorResources.GcsBucketSource_DownloadingError, gcsObject.Name), ex);
            }
        }
        #endregion
    }
}
