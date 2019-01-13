using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using StorageManagementKit.Diagnostics.Logging;
using StorageManagementKit.IO.FileSystem;
using StorageManagementKit.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StorageManagementKit.Core.Copying.Destinations
{
    public class AbsDestination : IRepositoryDestination
    {
        #region Members
        private string _containerName;

        private CloudStorageAccount _cloudStorage;
        private CloudBlobClient _blobClient;
        private CloudBlobContainer _blobContainer;
        #endregion

        #region Properties
        public ILogging Logger { get; set; }

        public string Description { get { return $"Blobs 'az://{_containerName}'"; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public AbsDestination(string containerName, string keyFile)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentNullException("containerName");

            if (string.IsNullOrWhiteSpace(keyFile))
                throw new ArgumentNullException("keyFile");

            _containerName = containerName.ToLower(); // Container names must be lowercase with Azure

            if (!CloudStorageAccount.TryParse(File.ReadAllText(keyFile), out _cloudStorage))
                throw new SmkException("Invalid Azure Access Key");

            _blobClient = _cloudStorage.CreateCloudBlobClient();
            _blobContainer = _blobClient.GetContainerReference(_containerName);
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Writes a file object into the destination repository.
        /// </summary>
        public bool Write(FileObject fo)
        {
            if (!fo.IsSecured)
            {
                Logger.WriteLog(ErrorCodes.AbsDestination_UnsecuredNotSupported,
                    ErrorResources.AbsDestination_UnsecuredNotSupported, Severity.Error, VerboseLevel.User);
                return false;
            }

            try
            {
                string destDataFile = $"{fo.Metadata.FullName}{Constants.EncryptedExt}"
                    .ToLower()
                    .Replace("\\", "/");

                if (destDataFile.StartsWith("/"))
                    destDataFile = destDataFile.Substring(1, destDataFile.Length - 1);

                CloudBlockBlob blob = _blobContainer.GetBlockBlobReference(destDataFile);

                blob.Metadata.Add(Constants.MetadataEncryptedKey.Replace(".", ""), fo.MetadataContent);
                blob.Metadata.Add(Constants.MetadataMD5Key, fo.MetadataMD5);
                blob.Metadata.Add(Constants.OriginalMD5Key, fo.Metadata.OriginalMD5);

                using (MemoryStream stream = new MemoryStream(fo.DataContent))
                {
                    stream.Position = 0;
                    byte[] data = stream.ToArray();
                    blob.UploadFromByteArrayAsync(data, 0, data.Length).Wait();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ErrorCodes.AbsDestination_CommitException, ex.Message, Severity.Error, VerboseLevel.User);
                return false;
            }
        }

        /// <summary>
        /// Returns all existing file objects into the repository
        /// </summary>
        public DiscoveredObject[] GetObjects()
        {
            try
            {
                Logger.WriteLog(ErrorCodes.AbsDestination_GettingObjectList,
                    ErrorResources.AbsDestination_GettingObjectList, Severity.Information, VerboseLevel.User);

                List<DiscoveredObject> list = new List<DiscoveredObject>();
                BlobContinuationToken blobContinuationToken = null;

                do
                {
                    var task = _blobContainer.ListBlobsSegmentedAsync(null, true, BlobListingDetails.None, null, null, null, null);
                    task.Wait();

                    blobContinuationToken = task.Result.ContinuationToken;
                    foreach (IListBlobItem item in task.Result.Results)
                    {
                        if (item is CloudBlockBlob)
                        {
                            string path = $"\\{ ((CloudBlockBlob)item).Name}".Replace("/", "\\");

                            list.Add(new DiscoveredObject()
                            {
                                DirectoryName = Path.GetDirectoryName(path),
                                FullName = path
                            });
                        }
                    }
                } while (blobContinuationToken != null);

                return list.ToArray();
            }
            catch
            {
                Logger.WriteLog(ErrorCodes.AbsDestination_GettingListException,
                    ErrorResources.AbsDestination_GettingListException, Severity.Error, VerboseLevel.User);

                throw;
            }
        }

        /// <summary>
        /// Deletes a file to the destination repository
        /// </summary>
        public bool Delete(string file, bool wideDisplay)
        {
            file = file.Replace("\\", "/");
            file = Helpers.RemoveRootSlash(file).ToLower();

            try
            {
                var blob = _blobContainer.GetBlockBlobReference(file);
                blob.DeleteIfExistsAsync();

                string displayName = Helpers.FormatDisplayFileName(wideDisplay, file);

                Logger.WriteLog(ErrorCodes.AbsDestination_FileDeleted,
                    $"del dst {displayName}", Severity.Information, VerboseLevel.User);

                return true;
            }
            catch
            {
                Logger.WriteLog(ErrorCodes.AbsDestination_DeleteException,
                    string.Format(ErrorResources.AbsDestination_DeleteException, file),
                    Severity.Error, VerboseLevel.User);

                throw;
            }
        }

        /// <summary>
        /// Determine source file object matches with the remote object.
        /// </summary>
        public bool IsMetadataMatch(string fullpath, bool isEncrypted, string sourceOriginalMd5)
        {
            string destDataFile = $"{fullpath}{Constants.EncryptedExt}".Replace("\\", "/").ToLower();

            if (destDataFile.StartsWith("/"))
                destDataFile = destDataFile.Substring(1, destDataFile.Length - 1);

            try
            {
                var task = _blobContainer.ListBlobsSegmentedAsync(
                    destDataFile, true, BlobListingDetails.Metadata, null, null, null, null);

                task.Wait();

                // The object must be unique
                if (task.Result.Results.Count() == 1)
                {
                    if (!(task.Result.Results.ToList()[0] is CloudBlockBlob))
                        return false;

                    CloudBlockBlob blob = (CloudBlockBlob)task.Result.Results.ToList()[0];

                    // Compares checksum for both local and azure blob object
                    string value;
                    if (blob.Metadata.TryGetValue(Constants.OriginalMD5Key, out value))
                        return value == sourceOriginalMd5;
                }

                return false;
            }
            catch
            {
                Logger.WriteLog(ErrorCodes.AbsDestination_IsMetadataMatchException,
                    string.Format(ErrorResources.AbsDestination_IsMetadataMatchException, fullpath),
                    Severity.Error, VerboseLevel.User);

                throw;
            }
        }

        /// <summary>
        /// This method is not used by this repository destination
        /// </summary>
        bool IRepositoryDestination.AfterDirectoryScan(string directory, bool wideDisplay)
        {
            return true; // Nothing to do!
        }
        #endregion
    }
}
