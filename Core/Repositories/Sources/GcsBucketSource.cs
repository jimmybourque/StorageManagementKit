using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.IO;
using StorageManagementKit.Core.Repositories.Destinations;
using StorageManagementKit.Core.Transforms;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace StorageManagementKit.Core.Repositories.Sources
{
    public class GcsBucketSource : IRepositorySource
    {
        #region Members
        public ILogging Logger { get; set; }
        private readonly IProgressing _progress;
        private int _synchronizedCount = 0;
        private int _ignoredCount = 0;
        private int _errorCount = 0;
        private int _deletedCount = 0;
        private long _writeSize = 0;
        private long _readSize = 0;
        private int _fileScanned = 0;
        private int _fileSynchronized = 0;
        private bool _wideDisplay;
        private StorageClient _client;
        #endregion

        #region Properties
        public string _bucketName { get; set; }
        public IRepositoryDestination Destination { get; set; }
        public ITransforming Transform { get; set; }
        public string Description { get { return $"GCS 'gs://{_bucketName}'"; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public GcsBucketSource(string bucketName, string oauthFile, IProgressing progress, bool wideDisplay)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException("bucketName");

            if (string.IsNullOrWhiteSpace(oauthFile))
                throw new ArgumentNullException("oauthFile");

            _progress = progress ?? throw new ArgumentNullException("progress");

            _bucketName = bucketName;
            _wideDisplay = wideDisplay;
            _client = StorageClient.Create(GoogleCredential.FromFile(oauthFile));
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Process all the objects from the repository and write them into the <see cref="Destination"/>
        /// </summary>
        public bool Process()
        {
            if (Destination == null)
                throw new SmkException($"'Destination' is not defined");

            if (Logger == null)
                throw new SmkException($"Property 'Logger' is not defined");

            _synchronizedCount = 0;
            _ignoredCount = 0;

            if (!ScanBucket())
            {
                ReportStats();
                return false;
            }
            ScanProgress(100, 100);

            Logger.WriteLine();

            if (!DeleteLocalGhostFiles())
            {
                ReportStats();
                return false;
            }
            ScanProgress(100, 100);

            ReportStats();
            return true;
        }
        #endregion

        #region Private methods
        private void ReportStats()
        {
            _progress.OnCompleted(_synchronizedCount, _ignoredCount, _errorCount, _deletedCount,
                _fileScanned, _fileSynchronized, _readSize, _writeSize);
        }

        /// <summary>
        /// Returns true if the file has been deleted
        /// </summary>
        private bool GcsObjectDelete(string objectName)
        {
            try
            {
                _client.DeleteObject(_bucketName, objectName.ToLower());
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
        /// Returns true if the object exists into the bucket
        /// </summary>
        private bool GcsObjectExists(string objectName)
        {
            try
            {
                // Only encrypted objects are stored
                objectName = $"{objectName}{Constants.EncryptedExt}";

                // Remove root slash
                if (objectName.StartsWith("/") || objectName.StartsWith("\\"))
                    objectName = objectName.Substring(1, objectName.Length - 1);

                objectName = objectName.Replace("\\", "/");

                return _client.GetObject(_bucketName, objectName.ToLower()) != null;
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
        /// Delete local (source) files that do not exist in the destination.
        /// </summary>
        private bool DeleteLocalGhostFiles()
        {
            Logger.WriteLog(ErrorCodes.SyncPhase_DeletionBegun2,
                ErrorResources.SyncPhase_DeletionBegun, Severity.Information, VerboseLevel.User, true);

            DiscoveredObject[] files = Destination.GetObjects();

            for (int i = 0; i < files.Length; i++)
            {
                ScanProgress(i, files.Length, files[i].FullName);

                if (files[i].Kind == ObjectKind.File)
                {
                    string gcsObject = Helpers.RemoveSecExt(files[i].FullName);

                    if (!GcsObjectExists(gcsObject))
                    {
                        if (Destination.Delete(gcsObject, _wideDisplay))
                            Interlocked.Increment(ref _deletedCount);
                        else
                            Interlocked.Increment(ref _errorCount);
                    }
                }
                else if (files[i].Kind == ObjectKind.Directory)
                {
                    if (!Destination.AfterDirectoryScan(files[i].FullName, _wideDisplay))
                        Interlocked.Increment(ref _errorCount);
                }
                else
                    throw new SmkException($"Unsupported type for {files[i].GetType().Name}.{files[i].Kind}");
            }

            Logger.WriteLog(ErrorCodes.SyncPhase_DeletionEnded2,
                ErrorResources.SyncPhase_DeletionEnded, Severity.Information, VerboseLevel.User, true);

            return _errorCount == 0;
        }

        /// <summary>
        /// Diagnostics each object returned by GCS
        /// </summary>
        private bool ScanBucket()
        {
            Logger.WriteLog(ErrorCodes.SyncPhase_SendingBegun2,
                ErrorResources.SyncPhase_SendingBegun, Severity.Information, VerboseLevel.User, true);

            Logger.WriteLog(ErrorCodes.GcsBucketSource_GettingObjectList,
                    ErrorResources.GcsBucketSource_GettingObjectList, Severity.Information, VerboseLevel.User);

            var list = _client.ListObjects(_bucketName).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                ScanProgress(i, list.Count, list[i].Name);

                if (!ProcessObject(list[i]))
                    break;
            }

            Logger.WriteLog(ErrorCodes.SyncPhase_SendingEnded2,
                ErrorResources.SyncPhase_SendingEnded, Severity.Information, VerboseLevel.User, true);

            return true;
        }

        /// <summary>
        /// The <see cref="DirectoryDiscover" /> has found a file
        /// </summary>
        private bool ProcessObject(Google.Apis.Storage.v1.Data.Object obj)
        {
            string originalName = Helpers.RemoveSecExt(obj.Name);

            string displayName = Helpers.FormatDisplayFileName(_wideDisplay, $"\\{originalName}");

            Logger.WriteLog(ErrorCodes.LocalDirectorySource_FileProcessing,
                $">> Processing {displayName}", Severity.Information, VerboseLevel.Debug);

            string originalMD5;
            string metadataMD5;
            string metadataEncrypted;

            if (!obj.Metadata.TryGetValue(Constants.OriginalMD5Key, out originalMD5) ||
                !obj.Metadata.TryGetValue(Constants.MetadataMD5Key, out metadataMD5) ||
                !obj.Metadata.TryGetValue(Constants.MetadataEncryptedKey, out metadataEncrypted))
            {
                Logger.WriteLog(ErrorCodes.GcsBucketSource_MissingMetadata,
                    string.Format(ErrorResources.GcsBucketSource_MissingMetadata, obj.Name),
                    Severity.Error, VerboseLevel.User);
                return false;
            }

            Interlocked.Increment(ref _fileScanned);
            Interlocked.Add(ref _readSize, (long)obj.Size);

            if (Destination.IsMetadataMatch($"\\{originalName}", Transform.IsSecured, originalMD5))
            {
                Logger.WriteLog(ErrorCodes.GcsBucketSource_IgnoredFile,
                    $">> Ignored {Helpers.FormatDisplayFileName(_wideDisplay, $"\\{originalName}")}", Severity.Information, VerboseLevel.Debug);

                Interlocked.Increment(ref _ignoredCount);
                return true;
            }

            FileObject fo = new FileObject()
            {
                Metadata = new FileMetadata() { OriginalMD5 = originalMD5 },
                DataContent = null,
                IsSecured = true,
                MetadataMD5 = metadataMD5,
                MetadataContent = metadataEncrypted
            };

            try
            {
                using (MemoryStream memStm = new MemoryStream())
                {
                    _client.DownloadObject(obj, memStm);
                    memStm.Position = 0;
                    fo.DataContent = memStm.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new SmkException(string.Format(ErrorResources.GcsBucketSource_DownloadingError, obj.Name), ex);
            }

            return Backup(obj, fo);
        }

        private bool Backup(Google.Apis.Storage.v1.Data.Object obj, FileObject fo)
        {
            string displayName = Helpers.FormatDisplayFileName(_wideDisplay, Helpers.RemoveSecExt(obj.Name));

            Logger.WriteLog(ErrorCodes.GcsBucketSource_SyncFile,
                $"cpy src {displayName} [{Helpers.FormatByteSize(fo.DataContent.Length)}]", Severity.Information, VerboseLevel.User);

            // Transforms the file if a transformation is provided
            FileObject fo2 = Transform != null ? Transform.Process(fo) : fo;

            bool result = Destination.Write(fo2);

            if (result)
            {
                Interlocked.Increment(ref _fileSynchronized);
                Interlocked.Add(ref _writeSize, fo2.DataContent.Length);
            }

            if (result)
                Interlocked.Increment(ref _synchronizedCount);
            else
                Interlocked.Increment(ref _errorCount);

            return result;
        }

        /// <summary>
        /// Progress stats from the <see cref="DirectoryDiscover" />
        /// </summary>
        public void ScanProgress(int progress, int total, string objectName = null)
        {
            _progress.OnProgress(progress, total, objectName);
        }
        #endregion
    }
}
