using StorageManagementKit.Core.Cleaning;
using StorageManagementKit.Core.Crypto;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.IO;
using StorageManagementKit.Core.Repositories.Destinations;
using StorageManagementKit.Core.Transforms;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace StorageManagementKit.Core.Repositories.Sources
{
    public class LocalDirectorySource : IRepositorySource, IDirectoryDiscovering
    {
        #region Members
        public ILogging Logger { get; set; }
        private string _path;
        private string _pathNoTail;
        private IProgressing _progress;
        private int _synchronizedCount = 0;
        private int _ignoredCount = 0;
        private int _errorCount = 0;
        private int _deletedCount = 0;
        private long _writeSize = 0;
        private long _readSize = 0;
        private int _fileScanned = 0;
        private int _fileSynchronized = 0;
        private bool _wideDisplay;
        private bool _ignoreCleaning;
        private CheckLevel _checkLevel;
        #endregion

        #region Properties
        public string Description
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"Local folder '{_path}'");
                sb.AppendLine($"Check level: {_checkLevel}");

                var cleaning = _ignoreCleaning ? "yes" : "no";
                sb.Append($"Ignore cleaning: {cleaning}");

                return sb.ToString();
            }
        }
        public IRepositoryDestination Destination { get; set; }
        public ITransforming Transform { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public LocalDirectorySource(string basePath, IProgressing progress, bool wideDisplay, CheckLevel checkLevel, bool ignoreCleaning)
        {
            _path = basePath ?? throw new ArgumentNullException("basePath");
            _pathNoTail = _path.RemoveTail();
            _progress = progress ?? throw new ArgumentNullException("progress");
            _wideDisplay = wideDisplay;
            _checkLevel = checkLevel;
            _ignoreCleaning = ignoreCleaning;
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

            if (!SynchronizeFiles())
            {
                ReportStats();
                return false;
            }
            ScanProgress(100, 100);

            Logger.WriteLine();

            if (!_ignoreCleaning)
            {
                if (!DeleteLocalGhostFiles())
                {
                    ReportStats();
                    return false;
                }
                ScanProgress(100, 100);

                Logger.WriteLine();

                if (!CleanArtefacts())
                {
                    ReportStats();
                    return false;
                }
                ScanProgress(100, 100);
            }

            ReportStats();
            return true;
        }

        /// <summary>
        /// Send all modified files to the repository destination 
        /// </summary>
        private bool SynchronizeFiles()
        {
            Logger.WriteLog(ErrorCodes.SyncPhase_SendingBegun,
                ErrorResources.SyncPhase_SendingBegun, Severity.Information, VerboseLevel.User, true);

            var exclusions = new string[]
            {
                $"{_pathNoTail}\\.smk-meta",
                $"{_pathNoTail}\\_smk-bin"
            };

            bool result = new DirectoryDiscover(_path, this, Logger, exclusions).Run();

            Logger.WriteLog(ErrorCodes.SyncPhase_SendingEnded,
                    ErrorResources.SyncPhase_SendingEnded, Severity.Information, VerboseLevel.User, true);

            return result;
        }

        /// <summary>
        /// Delete all no needed files located in the hive directory
        /// </summary>
        private bool CleanArtefacts()
        {
            Logger.WriteLog(ErrorCodes.SyncPhase_CleaningBegun,
                ErrorResources.SyncPhase_CleaningBegun, Severity.Information, VerboseLevel.User, true);

            bool result = new LocalDirectoryCleaner(_path, Logger, _progress, _wideDisplay).Process();

            Logger.WriteLog(ErrorCodes.SyncPhase_CleaningEnded,
                ErrorResources.SyncPhase_CleaningEnded, Severity.Information, VerboseLevel.User, true);

            return result;
        }

        /// <summary>
        /// Progress stats from the <see cref="DirectoryDiscover" />
        /// </summary>
        public void ScanProgress(int progress, int total, string objectName = null)
        {
            _progress.OnProgress(progress, total, objectName);
        }

        /// <summary>
        /// The <see cref="DirectoryDiscover" /> has found a file
        /// </summary>
        bool IDirectoryDiscovering.OnFileFound(FileInfo fi)
        {
            if ((_checkLevel == CheckLevel.ArchiveFlag) && ((fi.Attributes & FileAttributes.Archive) == 0))
                return true;

            try
            {
                // Ignore any file ending with '.md5.encrypted' and '.meta.encrypted'
                if (fi.Name.EndsWith($"{Constants.MD5Ext}") ||
                    fi.Name.EndsWith($"{Constants.MetadataExt}{Constants.EncryptedExt}"))
                    return true;

                if (Logger.LogFile.EndsWith(fi.Name))
                    return true; // Ignore the current log file

                Interlocked.Increment(ref _fileScanned);
                Interlocked.Add(ref _readSize, fi.Length);

                string displayName = Helpers.FormatDisplayFileName(_wideDisplay, fi.FullName.Replace(_path, ""));
                Logger.WriteLog(ErrorCodes.LocalDirectorySource_FileProcessing, $">> Processing {displayName}", Severity.Information, VerboseLevel.Debug);

                // Gets the original file name
                string originalName = Helpers.RemoveSecExt(fi.Name);
                // Gets the name without the extension '.encrypted'
                string originalFullname = Helpers.RemoveSecExt(fi.FullName);
                // Gets the hive path (\\.[hive]\...)
                string relativePath = originalFullname.Substring(_pathNoTail.Length + 1, originalFullname.Length - _pathNoTail.Length - 1);
                // Get file path of the digital signature of the original file
                string md5File = $"{_pathNoTail}\\{Constants.Hive}\\{relativePath}{Constants.MD5Ext}";


                FileObject fo = new FileObject() { DataContent = File.ReadAllBytes(fi.FullName) };
                fo.IsSecured = fi.FullName.EndsWith(Constants.EncryptedExt);

                fo.Metadata = new FileMetadata()
                {
                    Attributes = fi.Attributes & (FileAttributes.Hidden | FileAttributes.ReadOnly),
                    LastWriteTime = fi.LastWriteTime,
                    Name = originalName,
                    FullName = originalFullname.Replace(_pathNoTail, "") // Remove the base path; keep just the relative path
                };

                if (fo.IsSecured)
                {
                    string metaFile = $"{_pathNoTail}\\{Constants.Hive}\\{relativePath}{Constants.MetadataExt}{Constants.EncryptedExt}";

                    fo.MetadataContent = File.ReadAllText(metaFile);
                    fo.MetadataMD5 = MD5.GetMD5FromFile(md5File, MD5Kind.META, Logger);
                    fo.Metadata.OriginalMD5 = MD5.GetMD5FromFile(md5File, MD5Kind.DATA, Logger);
                }
                else
                {
                    fo.Metadata.OriginalMD5 = MD5.CreateHash(fo.DataContent);
                    fo.MetadataContent = fo.Metadata.ObjectToXmlUtf8();
                    fo.MetadataMD5 = MD5.CreateHash(fo.Metadata.ToBytes());

                    // Saves the signature on the source repository
                    Directory.CreateDirectory(Path.GetDirectoryName(md5File));
                }

                bool transformIsSecured = Transform == null ? false : Transform.IsSecured;
                string originalMD5 = GetOriginalMD5(fo, md5File);


                if (// The source repo does not have the signature
                    !File.Exists(md5File) ||

                   // The destination repo does not have the signature
                   ((_checkLevel == CheckLevel.RemoteMD5) &&
                    !Destination.IsMetadataMatch(fo.Metadata.FullName, transformIsSecured, originalMD5)) ||

                   // Check if the archived flag must be validated
                   ((_checkLevel == CheckLevel.ArchiveFlag) &&
                    ((fi.Attributes & FileAttributes.Archive) != 0)) ||

                   // The file has been modified since the last synchronization
                   ((_checkLevel != CheckLevel.ArchiveFlag) &&
                     MD5.GetMD5FromFile(md5File, MD5Kind.DATA, Logger) != fo.Metadata.OriginalMD5) ||

                   // The metadata has been modified since the last synchronization
                   ((_checkLevel != CheckLevel.ArchiveFlag) &&
                    MD5.GetMD5FromFile(md5File, MD5Kind.META, Logger) != fo.MetadataMD5))
                {
                    return Backup(fi, md5File, fo);
                }
                else
                {
                    Logger.WriteLog(ErrorCodes.LocalDirectorySource_IgnoredFile,
                        $">> Ignored {displayName}", Severity.Information, VerboseLevel.Debug);

                    Interlocked.Increment(ref _ignoredCount);
                    return true; // Else, just ignore the file
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ErrorCodes.LocalDirectorySource_FileFoundException, ex.Message, Severity.Error, VerboseLevel.User);

                Interlocked.Increment(ref _errorCount);
                return false; // Stop the scanning
            }
        }
        #endregion

        #region Private methods
        private void ReportStats()
        {
            _progress.OnCompleted(_synchronizedCount, _ignoredCount, _errorCount, _deletedCount,
                _fileScanned, _fileSynchronized, _readSize, _writeSize);
        }

        /// <summary>
        /// Delete local (source) files that do not exist in the destination.
        /// </summary>
        private bool DeleteLocalGhostFiles()
        {
            Logger.WriteLog(ErrorCodes.SyncPhase_DeletionBegun,
                ErrorResources.SyncPhase_DeletionBegun, Severity.Information, VerboseLevel.User, true);

            DiscoveredObject[] files = Destination.GetObjects();
            string currentDir = null;

            for (int i = 0; i < files.Length; i++)
            {
                string localFile = $"{_path}{files[i].FullName}";

                ScanProgress(i, files.Length, localFile);

                if (files[i].Kind == ObjectKind.File)
                {
                    if ((Transform != null) && (Transform.IsSecured))
                        localFile = Helpers.RemoveSecExt(localFile);
                    else if ((Transform != null) && (!Transform.IsSecured))
                        localFile = $"{localFile}{Constants.EncryptedExt}";

                    if (!File.Exists(localFile))
                    {
                        if (Destination.Delete(files[i].FullName, _wideDisplay))
                            Interlocked.Increment(ref _deletedCount);
                        else
                            Interlocked.Increment(ref _errorCount);
                    }

                    if ((currentDir == null) && (currentDir != files[i].DirectoryName))
                    {
                        if ((currentDir != null) && !Destination.AfterDirectoryScan(currentDir, _wideDisplay))
                            Interlocked.Increment(ref _errorCount);

                        currentDir = files[i].DirectoryName;
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

            Logger.WriteLog(ErrorCodes.SyncPhase_DeletionEnded,
                ErrorResources.SyncPhase_DeletionEnded, Severity.Information, VerboseLevel.User, true);

            return _errorCount == 0;
        }

        /// <summary>
        /// Returns the original MD5 signature by preferring the value from the metadata.
        /// Otherwise, the MD5 file is used.
        /// </summary>
        private string GetOriginalMD5(FileObject fo, string md5File)
        {
            if (!fo.IsSecured)
                return fo.Metadata.OriginalMD5;
            else
            {
                string[] lines = File.ReadAllLines(md5File);
                return lines[1].Replace("DATA:", "");
            }
        }

        /// <summary>
        /// Valid if the file has changed since the last synchronization
        /// 1. If the .md5 file is missing
        /// 2. If the digital signature is different
        /// -> then, sends the file to the destination
        /// </summary>
        private bool Backup(FileInfo fi, string md5File, FileObject fo)
        {
            string displayName = Helpers.FormatDisplayFileName(_wideDisplay, fi.FullName.Replace(_path, ""));
            Logger.WriteLog(ErrorCodes.LocalDirectorySource_SyncFile,
                $"cpy src {displayName} [{Helpers.FormatByteSize(fi.Length)}]", Severity.Information, VerboseLevel.User);

            // Transforms the file if a transformation is provided
            FileObject fo2 = Transform != null ? Transform.Process(fo) : fo;

            bool result = Destination.Write(fo2);

            if (result)
            {
                // Remove the archive flag
                fi.Attributes = fi.Attributes ^ FileAttributes.Archive;

                Interlocked.Increment(ref _fileSynchronized);
                Interlocked.Add(ref _writeSize, fo2.DataContent.Length);
            }

            if (result && !fo.IsSecured)
            {
                Interlocked.Increment(ref _synchronizedCount);

                if (File.Exists(md5File))
                    File.Delete(md5File);

                File.WriteAllText(md5File, $"META:{fo.MetadataMD5}{Environment.NewLine}DATA:{fo.Metadata.OriginalMD5}");
            }

            if (!result)
                Interlocked.Increment(ref _errorCount);

            return result;
        }
        #endregion
    }
}
