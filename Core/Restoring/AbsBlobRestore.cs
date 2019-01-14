using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using StorageManagementKit.Diagnostics.Logging;
using StorageManagementKit.Types;
using System;
using System.IO;
using System.Linq;

namespace StorageManagementKit.Core.Restoring
{
    public class AbsBlobRestore : IObjectRestoring
    {
        #region Members
        private long _fileSize;
        private const string _restorationBlobName = "_restorationBlobTmp";
        private string _containerName;
        private string _apiKey;
        private byte[] _crypto_key;
        private byte[] _crypto_iv;
        private ILogging _logger;
        private CloudStorageAccount _cloudStorage;
        private CloudBlobClient _blobClient;
        private CloudBlobContainer _blobContainer;
        #endregion

        #region Properties
        public string BucketName { get { return $"gs://{_containerName}"; } }
        #endregion

        #region Constructor
        /// <param name="containerName">Azure blob container name</param>
        /// <param name="apiKey">Connection string</param>
        /// <param name="crypto_key">3-DES key</param>
        /// <param name="crypto_iv">3-DES vector</param>
        public AbsBlobRestore(string containerName, string apiKey, byte[] crypto_key, byte[] crypto_iv, ILogging logger)
        {
            _containerName = containerName ?? throw new ArgumentNullException("containerName");
            _apiKey = apiKey ?? throw new ArgumentNullException("apiKey");
            _crypto_key = crypto_key ?? throw new ArgumentNullException("key");
            _crypto_iv = crypto_iv ?? throw new ArgumentNullException("iv");
            _logger = logger ?? throw new ArgumentNullException("logger");

            // Initialize Azure client instances
            if (!CloudStorageAccount.TryParse(File.ReadAllText(apiKey), out _cloudStorage))
                throw new SmkException("Invalid Azure Access Key");

            _blobClient = _cloudStorage.CreateCloudBlobClient();
            _blobContainer = _blobClient.GetContainerReference(_containerName);
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

                // Get all available snapshots of a blob
                var result = _blobContainer.ListBlobsSegmentedAsync(fmtObject, true, BlobListingDetails.All, null, null, null, null).Result.Results;

                // Keep just CloudBlockBlob objects
                var list = result.Where(i => i is CloudBlockBlob)
                    .Select(i => (CloudBlockBlob)i)
                    .ToArray();

                // Be sure the object exists
                if ((list == null) || (list.Count() == 0))
                {
                    _logger.WriteLog(ErrorCodes.AbsBlobRestore_ObjectNotFound,
                        string.Format(ErrorResources.AbsBlobRestore_ObjectNotFound, filename),
                        Severity.Error, VerboseLevel.User);
                    return null;
                }

                // Build the version list
                return list.Select(blob =>
                     new ObjectVersion()
                     {
                         Name = blob.Name,
                         TimeCreated = blob.Properties.LastModified.Value.DateTime.ToLocalTime(),
                         StorageClass = "           ",
                         Size = blob.Properties.Length,
                         VersionId = blob.SnapshotTime.HasValue ? blob.SnapshotTime.Value.DateTime.ToLocalTime() : (DateTime?)null,
                         ObjectData = blob
                     })
                    .OrderByDescending(a => a.TimeCreated)
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ErrorCodes.AbsObjectRestore_GetVersionsException,
                    ErrorResources.AbsObjectRestore_GetVersionsException + Environment.NewLine + ex.Message,
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
                CloudBlockBlob blob = (CloudBlockBlob)version.ObjectData;

                // TODO ignore _restorationBlobName on Getobjectlist
                // set the downloading progress stat
                var tmp = _blobContainer.GetBlobReference(_restorationBlobName);
                tmp.StartCopyAsync(blob.StorageUri.PrimaryUri).Wait();

                try
                {
                    using (MemoryStream memStm = new MemoryStream())
                    {
                        tmp.DownloadToStreamAsync(memStm).Wait();
                        memStm.Position = 0;
                        //   fo.DataContent = memStm.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    throw new SmkException(string.Format(ErrorResources.AbsContainerSource_DownloadingError, blob.Name), ex);
                }

                // Delete the artifact
                tmp.DeleteAsync();
                
                return true;

                //using (StorageClient _client = StorageClient.Create(GoogleCredential.FromFile(_apiKey)))
                //{
                //    var gen = new GetObjectOptions() { Generation = (long?)version.VersionId };
                //    var obj = _client.GetObject(_containerName, version.Name, gen);
                //    string originalMD5, metadataMD5, metadataEncrypted;

                //    if (!obj.Metadata.TryGetValue(Constants.OriginalMD5Key, out originalMD5) ||
                //        !obj.Metadata.TryGetValue(Constants.MetadataMD5Key, out metadataMD5) ||
                //        !obj.Metadata.TryGetValue(Constants.MetadataEncryptedKey, out metadataEncrypted))
                //    {
                //        _logger.WriteLog(ErrorCodes.GcsObjectRestore_MissingMetadata,
                //            string.Format(ErrorResources.GcsObjectRestore_MissingMetadata, obj.Name),
                //            Severity.Error, VerboseLevel.User);
                //        return false;
                //    }

                //    FileObject fo = new FileObject()
                //    {
                //        Metadata = new FileMetadata() { OriginalMD5 = originalMD5 },
                //        DataContent = null,
                //        IsSecured = true,
                //        MetadataMD5 = metadataMD5,
                //        MetadataContent = metadataEncrypted
                //    };

                //    DownloadObject(_client, obj, fo, version);

                //    Helpers.WriteProgress("Decrypting...");
                //    fo = new UnsecureTransform(_crypto_key, _crypto_iv, _logger).Process(fo);

                //    Helpers.WriteProgress("Saving...");
                //    Helpers.WriteProgress("");

                //    // Force a destination name if not given by the user
                //    if (string.IsNullOrEmpty(destination))
                //    {
                //        string destName = version.Name.Replace(Constants.EncryptedExt, "");
                //        destination = Path.GetFileNameWithoutExtension(destName);
                //        destination = $"{destination}.{version.VersionId}.restore{Path.GetExtension(destName)}";
                //    }

                //    File.WriteAllBytes(destination, fo.DataContent);
                //    File.SetAttributes(destination, fo.Metadata.Attributes);
                //    File.SetLastWriteTime(destination, fo.Metadata.LastWriteTime);

                //    return true;
                //}
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ErrorCodes.AbsBlobRestore_RestoreObjectException,
                    ErrorResources.AbsBlobRestore_RestoreObjectException + Environment.NewLine + ex.Message,
                    Severity.Error, VerboseLevel.User);
                return false;
            }
        }
        #endregion
    }
}
