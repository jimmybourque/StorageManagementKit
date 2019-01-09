using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StorageManagementKit.Core.Repositories.Destinations
{
    public class GcsBucketDestination : IRepositoryDestination
    {
        #region Members
        private string _bucketName;

        private StorageClient _client;
        #endregion

        #region Properties
        public ILogging Logger { get; set; }

        public string Description { get { return $"Bucket 'gs://{_bucketName}'"; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public GcsBucketDestination(string bucketName, string keyFile)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException("bucketName");

            if (string.IsNullOrWhiteSpace(keyFile))
                throw new ArgumentNullException("keyFile");

            _bucketName = bucketName;

            _client = StorageClient.Create(GoogleCredential.FromFile(keyFile));
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
                Logger.WriteLog(ErrorCodes.GcsBucketDestination_UnsecuredNotSupported,
                    ErrorResources.GcsBucketDestination_UnsecuredNotSupported, Severity.Error, VerboseLevel.User);
                return false;
            }

            try
            {
                string destDataFile = $"{fo.Metadata.FullName}{Constants.EncryptedExt}"
                    .ToLower()
                    .Replace("\\", "/");

                if (destDataFile.StartsWith("/"))
                    destDataFile = destDataFile.Substring(1, destDataFile.Length - 1);

                using (Stream stream = new MemoryStream(fo.DataContent))
                {
                    var fileobj = new Google.Apis.Storage.v1.Data.Object()
                    {
                        Name = destDataFile,
                        Metadata = new Dictionary<string, string>(),
                        Bucket = _bucketName
                    };

                    stream.Position = 0;

                    fileobj.Metadata.Add(Constants.MetadataEncryptedKey, fo.MetadataContent);
                    fileobj.Metadata.Add(Constants.MetadataMD5Key, fo.MetadataMD5);
                    fileobj.Metadata.Add(Constants.OriginalMD5Key, fo.Metadata.OriginalMD5);

                    _client.UploadObject(fileobj, stream);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ErrorCodes.GcsBucketDestination_CommitException, ex.Message, Severity.Error, VerboseLevel.User);
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
                Logger.WriteLog(ErrorCodes.GcsBucketDestination_GettingObjectList,
                    ErrorResources.GcsBucketDestination_GettingObjectList, Severity.Information, VerboseLevel.User);

                var list = _client.ListObjects(_bucketName);

                return list.Select(l =>
                {
                    string path = $"\\{l.Name}".Replace("/", "\\");

                    return new DiscoveredObject()
                    {
                        DirectoryName = Path.GetDirectoryName(path),
                        FullName = path
                    };
                }).ToArray();
            }
            catch
            {
                Logger.WriteLog(ErrorCodes.GcsBucketDestination_GettingListException,
                    ErrorResources.GcsBucketDestination_GettingListException, Severity.Error, VerboseLevel.User);

                throw;
            }
        }

        /// <summary>
        /// Deletes a file to the destination repository
        /// </summary>
        public bool Delete(string file, bool wideDisplay)
        {
            file = file.Replace("\\", "/");
            file = Helpers.RemoveRootSlash(file);

            try
            {
                _client.DeleteObject(_bucketName, file.ToLower());
                string displayName = Helpers.FormatDisplayFileName(wideDisplay, file);

                Logger.WriteLog(ErrorCodes.GcsBucketDestination_FileDeleted,
                    $"del dst {displayName}", Severity.Information, VerboseLevel.User);

                return true;
            }
            catch (Google.GoogleApiException ex)
            {
                if (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                else
                    throw;
            }
        }

        /// <summary>
        /// Determine source file object matches with the remote object.
        /// </summary>
        public bool IsMetadataMatch(string fullpath, bool isEncrypted, string sourceOriginalMd5)
        {
            string destDataFile = $"{fullpath}{Constants.EncryptedExt}".Replace("\\", "/");

            if (destDataFile.StartsWith("/"))
                destDataFile = destDataFile.Substring(1, destDataFile.Length - 1);

            try
            {
                var obj = _client.GetObject(_bucketName, destDataFile.ToLower());

                string value;
                if (obj.Metadata.TryGetValue(Constants.OriginalMD5Key, out value))
                    return value == sourceOriginalMd5;

                return false;
            }
            catch (Google.GoogleApiException ex)
            {
                if (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                else
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
