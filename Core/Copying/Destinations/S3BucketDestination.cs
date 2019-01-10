using Amazon.S3;
using Amazon.S3.Model;
using StorageManagementKit.Core.AWS;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.IO;
using System;
using System.IO;
using System.Linq;

namespace StorageManagementKit.Core.Copying.Destinations
{
    public class S3BucketDestination : IRepositoryDestination, IDisposable
    {
        #region Members
        private string _bucketName;
        private AmazonS3Client _client;
        private bool _isDisposed;
        #endregion

        #region Properties
        public ILogging Logger { get; set; }

        public string Description { get { return $"Bucket 's3://{_bucketName}'"; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public S3BucketDestination(string bucketName, string keyFile)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentNullException("bucketName");

            if (string.IsNullOrWhiteSpace(keyFile))
                throw new ArgumentNullException("keyFile");

            _bucketName = bucketName;

            S3Credentials credentials = S3Credentials.LoadKey(keyFile);
            _client = new AmazonS3Client(credentials.AccessKeyId, credentials.SecretAccessKey, credentials.AwsRegion);
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
                Logger.WriteLog(ErrorCodes.S3BucketDestination_UnsecuredNotSupported,
                    ErrorResources.S3BucketDestination_UnsecuredNotSupported, Severity.Error, VerboseLevel.User);
                return false;
            }

            try
            {
                string destDataFile = $"{fo.Metadata.FullName}{Constants.EncryptedExt}"
                    .ToLower()
                    .Replace("\\", "/");

                if (destDataFile.StartsWith("/"))
                    destDataFile = destDataFile.Substring(1, destDataFile.Length - 1);

                using (Stream stm = new MemoryStream(fo.DataContent))
                {
                    PutObjectRequest request = new PutObjectRequest()
                    {
                        BucketName = _bucketName,
                        InputStream = stm,
                        Key = destDataFile
                    };

                    request.Metadata.Add(Constants.MetadataEncryptedKey, fo.MetadataContent);
                    request.Metadata.Add(Constants.MetadataMD5Key, fo.MetadataMD5);
                    request.Metadata.Add(Constants.OriginalMD5Key, fo.Metadata.OriginalMD5);

                    _client.PutObjectAsync(request).Wait();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ErrorCodes.S3BucketDestination_CommitException, ex.Message, Severity.Error, VerboseLevel.User);
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
                Logger.WriteLog(ErrorCodes.S3BucketDestination_GettingObjectList,
                    ErrorResources.S3BucketDestination_GettingObjectList, Severity.Information, VerboseLevel.User);

                ListObjectsRequest request = new ListObjectsRequest()
                {
                    BucketName = _bucketName,
                    MaxKeys = int.MaxValue
                };

                ListObjectsResponse response = _client.ListObjectsAsync(request).Result;

                return response.S3Objects.Select(o =>
                {
                    string path = $"\\{o.Key}".Replace("/", "\\");

                    return new DiscoveredObject()
                    {
                        DirectoryName = Path.GetDirectoryName(path),
                        FullName = path
                    };
                }).ToArray();
            }
            catch
            {
                Logger.WriteLog(ErrorCodes.S3BucketDestination_GettingListException,
                    ErrorResources.S3BucketDestination_GettingListException, Severity.Error, VerboseLevel.User);

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
                DeleteObjectRequest request = new DeleteObjectRequest()
                {
                    BucketName = _bucketName,
                    Key = file.ToLower()
                };

                _client.DeleteObjectAsync(request).Wait();

                string displayName = Helpers.FormatDisplayFileName(wideDisplay, file);

                Logger.WriteLog(ErrorCodes.S3BucketDestination_FileDeleted,
                    $"del dst {displayName}", Severity.Information, VerboseLevel.User);

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
        /// Determine source file object matches with the remote object.
        /// </summary>
        public bool IsMetadataMatch(string fullpath, bool isEncrypted, string sourceOriginalMd5)
        {
            string destDataFile = $"{fullpath}{Constants.EncryptedExt}".Replace("\\", "/");

            if (destDataFile.StartsWith("/"))
                destDataFile = destDataFile.Substring(1, destDataFile.Length - 1);

            try
            {
                GetObjectMetadataRequest request = new GetObjectMetadataRequest()
                {
                    BucketName = _bucketName,
                    Key = destDataFile.ToLower()
                };

                GetObjectMetadataResponse response = _client.GetObjectMetadataAsync(request).Result;

                string metaMD5 = $"x-amz-meta-{Constants.OriginalMD5Key}".ToLower();

                if (response.Metadata.Keys.Any(m => m.Equals(metaMD5)))
                    return response.Metadata[metaMD5] == sourceOriginalMd5;

                return false;
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
        /// This method is not used by this repository destination
        /// </summary>
        bool IRepositoryDestination.AfterDirectoryScan(string directory, bool wideDisplay)
        {
            return true; // Nothing to do!
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _client.Dispose();
                _isDisposed = true;
            }
        }
        #endregion
    }
}
