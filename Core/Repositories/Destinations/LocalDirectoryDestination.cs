using StorageManagementKit.Core.Crypto;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.IO;
using StorageManagementKit.Core.Transforms;
using System;
using System.IO;

namespace StorageManagementKit.Core.Repositories.Destinations
{
    public class LocalDirectoryDestination : IRepositoryDestination
    {
        #region Members
        private string _path;
        private bool _persistSignature;
        private ITransforming _transform;
        #endregion

        #region Properties
        public ILogging Logger { get; set; }
        public string Description { get { return $"Local folder '{_path}'"; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public LocalDirectoryDestination(string path, bool persistSignature, ITransforming transform)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException("path");

            _transform = transform;
            _path = path;
            _persistSignature = persistSignature;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Writes a file object into the destination repository.
        /// </summary>
        public bool Write(FileObject fo)
        {
            try
            {
                string extSuffix = fo.IsSecured ? Constants.EncryptedExt : string.Empty;

                string destDataFile = $"{_path}{fo.Metadata.FullName}{extSuffix}";
                if (File.Exists(destDataFile))
                    File.Delete(destDataFile);

                string destMetaFile = BuildMetaFilePath(fo.Metadata.FullName);
                if (File.Exists(destMetaFile))
                    File.Delete(destMetaFile);

                string destMD5File = BuildMD5FilePath(fo.Metadata.FullName);
                if (File.Exists(destMD5File))
                    File.Delete(destMD5File);

                Directory.CreateDirectory(Path.GetDirectoryName(destDataFile));

                File.WriteAllBytes(destDataFile, fo.DataContent);

                if (fo.IsSecured)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destMetaFile));
                    File.WriteAllText(destMetaFile, fo.MetadataContent);
                }
                else
                {
                    File.SetLastWriteTimeUtc(destDataFile, fo.Metadata.LastWriteTime);
                    File.SetAttributes(destDataFile, fo.Metadata.Attributes);
                }

                if (_persistSignature)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destMD5File));
                    File.WriteAllText(destMD5File, $"META:{fo.MetadataMD5}{Environment.NewLine}DATA:{fo.Metadata.OriginalMD5}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ErrorCodes.LocalDirectoryDestination_CommitException, ex.Message, Severity.Error, VerboseLevel.User);
                return false;
            }
        }

        /// <summary>
        /// Returns all existing file objects into the repository
        /// </summary>
        public DiscoveredObject[] GetObjects()
        {
            var exclusions = new string[] { $"{_path.RemoveTail()}\\{Constants.Hive}", $"{_path.RemoveTail()}\\{Constants.Bin}" };
            var files = DirectoryDiscover.GetAllFiles(_path, exclusions, Logger);

            files.ForEach(f =>
            {
                f.DirectoryName = f.DirectoryName.Replace(_path, "").PrefixBackslash();
                f.FullName = f.FullName.Replace(_path, "").PrefixBackslash();
            });

            return files.ToArray();
        }

        /// <summary>
        /// Deletes a file to the destination repository
        /// </summary>
        public bool Delete(string file, bool wideDisplay)
        {
            try
            {
                string filePath = $"{_path}{file}";
                File.Delete(filePath);

                // Delete the signature file
                string destMD5File = BuildMD5FilePath(file);
                if (File.Exists(destMD5File))
                    File.Delete(destMD5File);

                // Delete the meta data file
                string destMetaFile = BuildMetaFilePath(file);
                if (File.Exists(destMetaFile))
                    File.Delete(destMetaFile);

                // Remove the directory if it is now empty
                string dir = Path.GetDirectoryName(destMD5File);
                if ((Directory.GetFiles(dir).Length == 0) && (Directory.GetDirectories(dir).Length == 0))
                    Directory.Delete(dir);

                // Write the log
                string displayName = Helpers.FormatDisplayFileName(wideDisplay, file.Replace(_path, ""));
                string extSuffix = _transform != null && _transform.IsSecured ? Constants.EncryptedExt : string.Empty;

                Logger.WriteLog(ErrorCodes.LocalDirectoryDestination_FileDeleted,
                    $"del dst {displayName}{extSuffix}", Severity.Information, VerboseLevel.User);

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ErrorCodes.LocalDirectoryDestination_DeletionFailed,
                    string.Format(ErrorResources.LocalDirectoryDestination_DeletionFailed, file),
                    Severity.Error, VerboseLevel.User);

                Logger.WriteLog(ErrorCodes.LocalDirectoryDestination_DeletionFailed,
                    ex.Message, Severity.Error, VerboseLevel.Debug);

                return false;
            }
        }

        /// <summary>
        /// Determine if the source file object matches with the remote object.
        /// </summary>
        public bool IsMetadataMatch(string fullpath, bool isEncrypted, string sourceOriginalMd5)
        {
            string localMD5File = BuildMD5FilePath(fullpath);
            string securedExt = isEncrypted ? Constants.EncryptedExt : string.Empty;
            string localPath = $"{_path}{fullpath}{securedExt}";

            if (!File.Exists(localMD5File))
                return false; // Missing signature

            // For optimization, compares the md5 file firt
            if (MD5.GetMD5FromFile(localMD5File, MD5Kind.DATA, Logger) != sourceOriginalMd5)
                return false;

            // If the file is missing, it must be synchronized
            if (!File.Exists(localPath))
                return false;

            // Otherwise, calculate a signature from the original file
            string localOriginalMd5 = null;

            if (isEncrypted)
            {
                TripleDES des = new TripleDES(_transform.Key, _transform.IV);
                localOriginalMd5 = MD5.CreateHash(des.Decrypt(File.ReadAllBytes(localPath)));
            }
            else
                localOriginalMd5 = MD5.CreateHash(File.ReadAllBytes(localPath));

            if (localOriginalMd5 != sourceOriginalMd5)
                return false;

            return true;
        }

        /// <summary>
        /// Deletes the folder if it is now empty after the content deletion step
        /// </summary>
        bool IRepositoryDestination.AfterDirectoryScan(string directory, bool wideDisplay)
        {
            string folder = $"{_path}{directory}";

            if ((Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Length == 0) &&
                (Directory.GetDirectories(folder).Length == 0))
                try
                {
                    // Delete the folder
                    Directory.Delete(folder, true);

                    // Delete the hive sub folder
                    string jboDir = $"{_path}\\{Constants.Hive}{directory}";

                    if (Directory.Exists(jboDir))
                        Directory.Delete(jboDir, true);

                    Logger.WriteLog(ErrorCodes.LocalDirectoryDestination_DirectoryDeleted,
                        $"rmdir dst {Helpers.FormatDisplayFileName(wideDisplay, directory)}", Severity.Information, VerboseLevel.User);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ErrorCodes.LocalDirectoryDestination_DirectoryDeletionException,
                        string.Format(ErrorResources.LocalDirectoryDestination_DirectoryDeletionException, directory) +
                        $"{Environment.NewLine}{ex.Message}",
                        Severity.Error, VerboseLevel.User);
                }

            return true; // The folder is not empty
        }
        #endregion

        #region Helper methods
        private string BuildMD5FilePath(string filepath)
        {
            return $"{_path}\\{Constants.Hive}{filepath}{Constants.MD5Ext}";
        }

        private string BuildMetaFilePath(string filepath)
        {
            return BuildMetaFilePath(filepath, _transform != null && _transform.IsSecured);
        }

        private string BuildMetaFilePath(string filepath, bool isSecured)
        {
            string extSuffix = isSecured ? Constants.EncryptedExt : string.Empty;
            return $"{_path}\\{Constants.Hive}{filepath}{Constants.MetadataExt}{extSuffix}";
        }
        #endregion
    }
}
