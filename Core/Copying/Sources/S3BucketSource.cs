using Amazon.S3;
using Amazon.S3.Model;
using StorageManagementKit.Core.AWS;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.IO;
using StorageManagementKit.Core.Copying.Destinations;
using StorageManagementKit.Core.Transforms;
using System;
using System.IO;
using System.Threading;

namespace StorageManagementKit.Core.Copying.Sources
{
    public class S3BucketSource : IRepositorySource
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
        private AmazonS3Client _client;
        #endregion

        #region Properties
        public string _bucketName { get; set; }
        public IRepositoryDestination Destination { get; set; }
        public ITransforming Transform { get; set; }
        public string Description { get { return $"Bucket 's3://{_bucketName}'"; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public S3BucketSource(string bucketName, string keyFile, IProgressing progress, bool wideDisplay)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException("bucketName");

            if (string.IsNullOrWhiteSpace(keyFile))
                throw new ArgumentNullException("keyFile");

            _progress = progress ?? throw new ArgumentNullException("progress");

            _bucketName = bucketName;
            _wideDisplay = wideDisplay;

            S3Credentials credentials = S3Credentials.LoadKey(keyFile);
            _client = new AmazonS3Client(credentials.AccessKeyId, credentials.SecretAccessKey, credentials.AwsRegion);
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
        private bool ObjectDelete(string objectName)
        {
            try
            {
                DeleteObjectRequest request = new DeleteObjectRequest()
                {
                    BucketName = _bucketName,
                    Key = objectName.ToLower()
                };

                _client.DeleteObjectAsync(request).Wait();
                return true;
            }
            catch (AggregateException ex)
            {
                if ((ex.InnerExceptions.Count == 1) && (ex.InnerExceptions[0] is AmazonS3Exception))
                {
                    // The object does not exist into the bucket
                    if ((ex.InnerExceptions[0] as AmazonS3Exception).ErrorCode.ToLower() == "notfound")
                        return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Returns true if the object exists into the bucket
        /// </summary>
        private bool ObjectExists(string objectName)
        {
            try
            {
                // Only encrypted objects are stored
                objectName = $"{objectName}{Constants.EncryptedExt}";

                // Remove root slash
                if (objectName.StartsWith("/") || objectName.StartsWith("\\"))
                    objectName = objectName.Substring(1, objectName.Length - 1);

                objectName = objectName.Replace("\\", "/");

                GetObjectMetadataRequest request = new GetObjectMetadataRequest()
                {
                    BucketName = _bucketName,
                    Key = objectName.ToLower()
                };

                GetObjectMetadataResponse response = _client.GetObjectMetadataAsync(request).Result;
                return true;
            }
            catch (AggregateException ex)
            {
                if ((ex.InnerExceptions.Count == 1) && (ex.InnerExceptions[0] is AmazonS3Exception))
                {
                    // The object does not exist into the bucket
                    if ((ex.InnerExceptions[0] as AmazonS3Exception).ErrorCode.ToLower() == "notfound")
                        return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Delete local (source) files that do not exist in the destination.
        /// </summary>
        private bool DeleteLocalGhostFiles()
        {
            Logger.WriteLog(ErrorCodes.SyncPhase_DeletionBegun3,
                ErrorResources.SyncPhase_DeletionBegun, Severity.Information, VerboseLevel.User, true);

            DiscoveredObject[] files = Destination.GetObjects();

            for (int i = 0; i < files.Length; i++)
            {
                ScanProgress(i, files.Length, files[i].FullName);

                if (files[i].Kind == ObjectKind.File)
                {
                    string s3Object = Helpers.RemoveSecExt(files[i].FullName);

                    if (!ObjectExists(s3Object))
                    {
                        if (Destination.Delete(s3Object, _wideDisplay))
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

            Logger.WriteLog(ErrorCodes.SyncPhase_DeletionEnded3,
                ErrorResources.SyncPhase_DeletionEnded, Severity.Information, VerboseLevel.User, true);

            return _errorCount == 0;
        }

        /// <summary>
        /// Diagnostics each object returned by S3
        /// </summary>
        private bool ScanBucket()
        {
            Logger.WriteLog(ErrorCodes.SyncPhase_SendingBegun3,
                ErrorResources.SyncPhase_SendingBegun, Severity.Information, VerboseLevel.User, true);

            Logger.WriteLog(ErrorCodes.S3BucketSource_GettingObjectList,
                    ErrorResources.S3BucketSource_GettingObjectList, Severity.Information, VerboseLevel.User);

            // Gets the summary about an object
            ListObjectsRequest request = new ListObjectsRequest()
            {
                BucketName = _bucketName,
                MaxKeys = int.MaxValue
            };

            ListObjectsResponse list = _client.ListObjectsAsync(request).Result;

            for (int i = 0; i < list.S3Objects.Count; i++)
            {
                ScanProgress(i, list.S3Objects.Count, list.S3Objects[i].Key);

                // Gets the meta data about an object
                var meta = _client.GetObjectMetadataAsync(new GetObjectMetadataRequest()
                {
                    BucketName = _bucketName,
                    Key = list.S3Objects[i].Key
                }).Result.Metadata;

                if (!ProcessObject(list.S3Objects[i], meta))
                    break;
            }

            Logger.WriteLog(ErrorCodes.SyncPhase_SendingEnded3,
                ErrorResources.SyncPhase_SendingEnded, Severity.Information, VerboseLevel.User, true);

            return true;
        }

        /// <summary>
        /// The <see cref="DirectoryDiscover" /> has found a file
        /// </summary>
        private bool ProcessObject(S3Object obj, MetadataCollection metadata)
        {
            string originalName = Helpers.RemoveSecExt(obj.Key);

            string displayName = Helpers.FormatDisplayFileName(_wideDisplay, $"\\{originalName}");

            Logger.WriteLog(ErrorCodes.LocalDirectorySource_FileProcessing,
                $">> Processing {displayName}", Severity.Information, VerboseLevel.Debug);

            // Extracts the metadata values
            string originalMD5 = null, metadataMD5 = null, metadataEncrypted = null;
            if (!ExtractMetadata(obj, metadata, out originalMD5, out metadataMD5, out metadataEncrypted))
                return false;

            Interlocked.Increment(ref _fileScanned);
            Interlocked.Add(ref _readSize, (long)obj.Size);

            if (Destination.IsMetadataMatch($"\\{originalName}", Transform.IsSecured, originalMD5))
            {
                Logger.WriteLog(ErrorCodes.S3BucketSource_IgnoredFile,
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
                GetObjectRequest getRequest = new GetObjectRequest()
                {
                    BucketName = _bucketName,
                    Key = obj.Key
                };

                using (GetObjectResponse response = _client.GetObjectAsync(getRequest).Result)
                {
                    // Extracts the file content
                    using (MemoryStream stm = new MemoryStream())
                    {
                        response.ResponseStream.CopyTo(stm);
                        stm.Position = 0;
                        fo.DataContent = stm.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SmkException(string.Format(ErrorResources.S3BucketSource_DownloadingError, obj.Key), ex);
            }

            return Backup(obj, fo);
        }

        /// <summary>
        /// Extracts the metadata values from the collection.
        /// </summary>
        private bool ExtractMetadata(S3Object obj, MetadataCollection metadata, out string originalMD5, out string metadataMD5, out string metadataEncrypted)
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
                Logger.WriteLog(ErrorCodes.S3BucketSource_MissingMetadata,
                    string.Format(ErrorResources.S3BucketSource_MissingMetadata, obj.Key),
                    Severity.Error, VerboseLevel.User);
                return false;
            }

            return true;
        }

        private bool Backup(S3Object obj, FileObject fo)
        {
            string displayName = Helpers.FormatDisplayFileName(_wideDisplay, Helpers.RemoveSecExt(obj.Key));

            Logger.WriteLog(ErrorCodes.S3BucketSource_SyncFile,
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
