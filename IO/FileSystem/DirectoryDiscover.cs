using StorageManagementKit.Diagnostics.Logging;
using StorageManagementKit.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace StorageManagementKit.IO.FileSystem
{
    public class DirectoryDiscover
    {
        private readonly string _path;
        private readonly IDirectoryDiscovering _dirDisco;
        private readonly ILogging _logger;
        private readonly string[] _exclusionPathes;
        private int _current;
        private int _maximum;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">The path to scan all files</param>
        /// <param name="dirSync">Instance that implement a <see cref="IDirectoryDiscovering">discover</see></param>
        /// <param name="logger">Instance that implement a <see cref="ILogging">logger</see></param>
        /// <param name="exclusionPathes">Base pathes that must be excluded of the scan</param>
        public DirectoryDiscover(string path, IDirectoryDiscovering dirSync, ILogging logger, string[] exclusionPathes)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException();

            if (!Directory.Exists(path))
                throw new IOException($"The path {path} could not be found");

            _path = path;
            _dirDisco = dirSync ?? throw new ArgumentNullException("del");
            _logger = logger ?? throw new ArgumentNullException("logger");

            _exclusionPathes = exclusionPathes != null ? exclusionPathes.Select(e => e.ToLower()).ToArray() : null;
        }

        /// <summary>
        /// Scan the folder and subfolders to list files.
        /// </summary>
        public bool Run()
        {
            _logger.WriteLog(ErrorCodes.DirectoryDiscover_GettingFileList,
                ErrorResources.DirectoryDiscover_GettingFileList, Severity.Information, VerboseLevel.User);

            _maximum = GetAllFiles(_path, _exclusionPathes, _logger).Count;

            return ScanDirectory(new DirectoryInfo(_path));
        }

        /// <summary>
        /// Scan each directories and files into the folder
        /// </summary>
        private bool ScanDirectory(DirectoryInfo di)
        {
            DirectoryInfo[] dObjects = di.GetDirectories();

            foreach (var dObject in dObjects)
                if (!IsExcluded(dObject, _exclusionPathes, _logger))
                    if (!ScanDirectory(dObject))
                        return false;

            if (!ScanFiles(di))
                return false;

            return true;
        }

        /// <summary>
        /// Scan each files into the folder
        /// </summary>
        private bool ScanFiles(DirectoryInfo di)
        {
            FileInfo[] fObjects = di.GetFiles();

            foreach (var fObject in fObjects)
            {
                Interlocked.Increment(ref _current);
                _dirDisco.ScanProgress(_current, _maximum, fObject.FullName);

                // The object must be a user file
                if ((fObject.Attributes & (FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.Normal | FileAttributes.Archive)) != 0)
                {
                    if (!_dirDisco.OnFileFound(fObject))
                    {
                        _logger.WriteLog(ErrorCodes.DirectorySync_ScanInterrupted,
                            ErrorResources.DirectorySync_ScanInterrupted, Severity.Error, VerboseLevel.User);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Returns all files by excluding any special or system folder and any exclusion 
        /// defined by the <see cref="exclusionPathes"/> parameter
        /// </summary>
        public static List<DiscoveredObject> GetAllFiles(string path, string[] exclusionPathes, ILogging logger)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (logger == null) throw new ArgumentNullException("logger");

            if (exclusionPathes != null)
                exclusionPathes = (from e in exclusionPathes select e.ToLower()).ToArray();

            DirectoryInfo di = new DirectoryInfo(path);
            List<DiscoveredObject> objectsFiles = new List<DiscoveredObject>();

            objectsFiles.AddRange(di.GetFiles("*", SearchOption.TopDirectoryOnly).Select(f => f.ToDiscoveredObject()));

            GetFiles(exclusionPathes, logger, di, objectsFiles);

            return objectsFiles;
        }

        /// <summary>
        /// Recursive method to scan each folders
        /// </summary>
        private static void GetFiles(string[] exclusionPathes, ILogging logger,
            DirectoryInfo dirInfo, List<DiscoveredObject> objectsFound)
        {
            foreach (DirectoryInfo subDir in dirInfo.GetDirectories("*"))
            {
                if (!IsExcluded(subDir, exclusionPathes, logger))
                {
                    try
                    {
                        objectsFound.AddRange(subDir
                            .GetFiles("*", SearchOption.TopDirectoryOnly)
                            .Select(f => f.ToDiscoveredObject()));

                        GetFiles(exclusionPathes, logger, subDir, objectsFound);
                    }
                    catch (Exception ex)
                    {
                        throw new SmkException($"Failed to access {subDir.FullName}", ex);
                    }

                    objectsFound.Add(subDir.ToDiscoveredObject());
                }
            }
        }

        /// <summary>
        /// Returns true if the folder is excluded of the discovering
        /// </summary>
        private static bool IsExcluded(DirectoryInfo subDir, string[] exclusionPathes, ILogging logger)
        {
            if (subDir.FullName.EndsWith("$RECYCLE.BIN"))
                return true;

            if (subDir.FullName.EndsWith("System Volume Information"))
                return true;

            if ((subDir.Attributes & FileAttributes.System) != 0)
            {
                logger.WriteLog(0, $"Folder ignored '{subDir.FullName}", Severity.Information, VerboseLevel.Debug);
                return true;
            }

            if (IsExcludedRule(subDir, exclusionPathes))
            {
                logger.WriteLog(0, $"Folder excluded '{subDir.FullName}", Severity.Information, VerboseLevel.Debug);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the file is located into any excluded folder
        /// </summary>
        private static bool IsExcludedRule(DirectoryInfo di, string[] exclusionPathes)
        {
            if (exclusionPathes == null)
                return false;

            return exclusionPathes.Any(e => di.FullName.ToLower().StartsWith($@"{e}\") || di.FullName.ToLower().Equals(e));
        }
    }
}
