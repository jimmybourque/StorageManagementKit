using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.IO;
using System;
using System.IO;

namespace StorageManagementKit.Core.Cleaning
{
    public class LocalDirectoryCleaner : IDirectoryDiscovering
    {
        private string _path;
        private ILogging _logger;
        private IProgressing _progress;
        private bool _wideDisplay;

        public LocalDirectoryCleaner(string path, ILogging logger, IProgressing progress, bool wideDisplay)
        {
            _path = path ?? throw new ArgumentNullException("path");
            _logger = logger ?? throw new ArgumentNullException("logger");
            _progress = progress ?? throw new ArgumentNullException("progress");
            _wideDisplay = wideDisplay;
        }

        /// <summary>
        /// Process the hive directory scan
        /// </summary>
        public bool Process()
        {
            return new DirectoryDiscover($"{_path}\\{Constants.Hive}", this, _logger, null).Run();
        }

        /// <summary>
        /// The <see cref="DirectoryDiscover"/> has found a file
        /// </summary>
        public bool OnFileFound(FileInfo fi)
        {
            string originalFile = fi.FullName.Replace($"\\{Constants.Hive}", "");

            if (originalFile.EndsWith(Constants.MD5Ext))
                originalFile = originalFile.Substring(0, originalFile.Length - Constants.MD5Ext.Length);

            if (originalFile.EndsWith(Constants.MetadataExt))
                originalFile = originalFile.Substring(0, originalFile.Length - Constants.MetadataExt.Length);

            if (originalFile.EndsWith(Constants.MetadataExt + Constants.EncryptedExt))
                originalFile = originalFile.Substring(0, originalFile.Length - Constants.MetadataExt.Length - Constants.EncryptedExt.Length);

            if (!File.Exists(originalFile) && !File.Exists($"{originalFile}{Constants.EncryptedExt}"))
            {
                // File with the ".md5" extension
                if (!DeleteFile(fi.FullName))
                    return false;

                // File with the ".meta" extension
                string metaFile = Path.ChangeExtension(fi.FullName, Constants.MetadataExt);
                if (File.Exists(metaFile))
                    if (!DeleteFile(metaFile))
                        return false;

                // File with the ".encrypted" extension
                metaFile = metaFile + Constants.EncryptedExt;
                if (File.Exists(metaFile))
                    if (!DeleteFile(metaFile))
                        return false;
            }

            return true;
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        private bool DeleteFile(string filename)
        {
            try
            {
                File.Delete(filename);

                _logger.WriteLog(ErrorCodes.LocalDirectoryCleaner_DeletedFile,
                    $"del dst {Helpers.FormatDisplayFileName(_wideDisplay, filename)}",
                    Severity.Information, VerboseLevel.User);

                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ErrorCodes.LocalDirectoryCleaner_FailedToDeleteFile,
                    string.Format(ErrorResources.LocalDirectoryCleaner_FailedToDeleteFile, filename) + Environment.NewLine + ex.Message,
                    Severity.Error, VerboseLevel.User);
                return false;
            }
        }

        /// <summary>
        /// Progress stats from the <see cref="DirectoryDiscover" />
        /// </summary>
        public void ScanProgress(int progress, int total, string objectName = null)
        {
            _progress.OnProgress(progress, total, objectName);
        }
    }
}
