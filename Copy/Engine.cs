using StorageManagementKit.Core;
using StorageManagementKit.Core.Cleaning;
using StorageManagementKit.Core.Copying.Destinations;
using StorageManagementKit.Core.Copying.Sources;
using StorageManagementKit.Core.Crypto;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Transforms;
using System;
using System.Linq;

namespace StorageManagementKit.Copy
{
    public class Engine : IProgressing, IDisposable
    {
        #region Members
        private string[] _arguments;
        private Logger _logger;
        private bool _disposed = false;
        private const string _title = "SMK-Copy";
        #endregion

        #region Constructors
        public Engine(string[] args)
        {
            _arguments = args ?? throw new ArgumentNullException("args");

            Console.Title = _title;
            InitLogFile();
        }

        ~Engine()
        {
            Dispose();
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Init the log file
        /// </summary>
        private void InitLogFile()
        {
            try
            {
                var verboseLevel = _arguments.Any(a => a.ToLower() == $"/{Arguments.Debug}") ? VerboseLevel.Debug : VerboseLevel.User;

                string logfile = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.Log);

                if (string.IsNullOrEmpty(logfile)) // Set the default filename
                    logfile = "trace.log";

                string age = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.LogAge);
                if (string.IsNullOrEmpty(age))
                    age = "9";

                int iage;
                if (!int.TryParse(age, out iage))
                {
                    Console.WriteLine(ErrorResources.Engine_InvalidLogAge);
                    Environment.Exit(-1);
                }

                _logger = new Logger(logfile, iage, verboseLevel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public void Process()
        {
            // Display the help
            if (_arguments.Any(a => a.Equals("?") || a.Equals("-help")))
                DisplayHelp();

            else if (!ConsoleHelpers.AllCommandExist(_arguments, typeof(Arguments)))
                return;

            // Proceed a cleanup of the folder .smk-meta
            else if (ConsoleHelpers.CommandArgExists(_arguments, Arguments.RemoveArtifacts))
                CleanArtefacts();

            // Generate a new key
            else if (ConsoleHelpers.CommandArgExists(_arguments, Arguments.NewKey))
            {
                DisplayHeader();
                string value = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.NewKey);
                TripleDES.GenerateKey(value, _logger);
                _logger.WriteLine(string.Format(ErrorResources.Engine_KeyFileGenerated, value));
                DisplayFooter();
            }
            // Run a backup
            else
                Backup();
        }

        /// <summary>
        /// Delete all unused files into the .smk-hive directory
        /// </summary>
        private void CleanArtefacts()
        {
            DisplayHeader();

            _logger.WriteLine("Cleaning started...");

            if (!ConsoleHelpers.CommandArgExists(_arguments, Arguments.SourcePath))
            {
                _logger.WriteError($"Missing argument {Arguments.SourcePath}");
                return;
            }

            bool wideDisplay = ConsoleHelpers.CommandArgExists(_arguments, Arguments.Wide);

            string path = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.SourcePath);
            new LocalDirectoryCleaner(path, _logger, this, wideDisplay).Process();

            DisplayFooter();
        }

        /// <summary>
        /// Backup a repository in according to the source and destination.
        /// </summary>
        public void Backup()
        {
            try
            {
                DisplayHeader();

                SourceSettings srcSettings = new SourceSettings()
                {
                    Repository = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.Source).ConvertToSourceRepository(),
                    Path = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.SourcePath),
                    CheckLevel = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.Check).ConvertToCheckLevel(),
                    NoCleaning = ConsoleHelpers.CommandArgExists(_arguments, Arguments.NoCleaning),
                    WideDisplay = ConsoleHelpers.CommandArgExists(_arguments, Arguments.Wide),
                    ApiKey = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.SourceApiKey)
                };

                DestinationSettings dstSettings = new DestinationSettings()
                {
                    Repository = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.Dest).ConvertToDestinationRepository(),
                    Path = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.DestPath),
                    ApiKey = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.DestApiKey)
                };

                TransformSettings trfSettings = new TransformSettings()
                {
                    Kind = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.Transform).ConvertToTransformKind(),
                    TripleDesFilename = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.CryptoKey)
                };

                IRepositorySource source = new RepositorySourceFactory(_logger, this).Create(srcSettings, dstSettings, trfSettings);
                WriteComponentLabels(source);
                source.Process();

                DisplayFooter();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Display the help reference to the user
        /// </summary>
        public void DisplayHelp()
        {
            DisplayHeader();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Source");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.Source}\t\t[{SourceRepository.Local}|{SourceRepository.GCS}|{SourceRepository.S3}] Local drive directory, Google Cloud Storage or Amazon");
            Console.WriteLine($"/{Arguments.SourcePath}\tThe folder path, the bucket name");
            Console.WriteLine($"/{Arguments.SourceApiKey}\tThe file that contains the API key to access the bucket (if /{Arguments.Source} <> local)");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"Reliability");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"\tOnly if {Arguments.Source}={SourceRepository.Local}");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.Check}\t\t[{CheckLevel.LocalMD5}] (default) only local signature is used to detect a change, if possible");
            Console.WriteLine($"\t\t[{CheckLevel.RemoteMD5}] source and destination signatures are used");
            Console.WriteLine($"\t\t[{CheckLevel.ArchiveFlag}] only the archive flag is used");
            Console.WriteLine($"/{Arguments.NoCleaning}\tDo not delete destination files that do no more exist on the source repository");

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Destination");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.Dest}\t\t[{DestinationRepository.Local}|{DestinationRepository.GCS}|{DestinationRepository.S3}] Local drive directory, Google Cloud Storage or Amazon");
            Console.WriteLine($"/{Arguments.DestPath}\tThe folder path, the bucket name");
            Console.WriteLine($"/{Arguments.DestApiKey}\tThe file that contains the API key to access the bucket (if /{Arguments.Dest} <> local)");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Transformation");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.Transform}\t[{TransformKind.Secure}|{TransformKind.Unsecure}|{TransformKind.None}] Encrypt or decrypt with a 3-DES algorithm");
            Console.WriteLine($"/{Arguments.CryptoKey}\tThe file path that contains the key if [transform=none]");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Troubleshooting");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.Debug}\t\tDisplay all informations");
            Console.WriteLine($"/{Arguments.Wide}\t\tDisplay the full path of each object");
            Console.WriteLine($"/{Arguments.Log}\t\tThe log file name");
            Console.WriteLine($"/{Arguments.LogAge}\t\tThe number of log histories to keep (default=9)");
            Console.WriteLine($"/{Arguments.RemoveArtifacts}\tClean obsolete files into the local repository folder '{Constants.Hive}' define by the argument /{Arguments.SourcePath}");

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Crypto");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.NewKey}\t\tThe path of the new key file");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Examples");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Generate a new 3-DES crypto key");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.NewKey}=keyfile.json");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Backup and encrypt a local folder to GCS");
            Console.ForegroundColor = ConsoleColor.White;

            string ex1 = $@"/{Arguments.Source}={SourceRepository.Local} /{Arguments.SourcePath}=C:\repo " +
                $@"/{Arguments.Dest}={DestinationRepository.GCS} /{Arguments.DestPath}=gcs-repo-name " +
                $@"/{Arguments.DestApiKey}=apikey.json " + 
                $@"/{Arguments.Transform}={TransformKind.Secure} /{Arguments.CryptoKey}=crypto.key " + 
                $@"/{Arguments.Log}=logs\trace.log /{Arguments.LogAge}=5 " + 
                $@"/{Arguments.Check}={CheckLevel.RemoteMD5}";

            Console.WriteLine(ex1);
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Restore a GCS bucket folder to a local folder and decrypt files");
            Console.ForegroundColor = ConsoleColor.White;

            string ex2 = $@"/{Arguments.Source}={SourceRepository.GCS} /{Arguments.SourcePath}=gcs-repo-name " +
                $@"/{Arguments.SourceApiKey}=apikey.json " +
                $@"/{Arguments.Dest}={DestinationRepository.Local} /{Arguments.DestPath}=C:\repo " +
                $@"/{Arguments.Transform}={TransformKind.Unsecure} /{Arguments.CryptoKey}=crypto.key " +
                $@"/{Arguments.Log}=logs\trace.log /{Arguments.LogAge}=5";

            Console.WriteLine(ex2);
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
        }
        #endregion

        #region Diagnostic methods
        /// <summary>
        /// Report the scan progress to the user
        /// </summary>
        public void OnProgress(int progress, int total, string objectName = null)
        {
            Console.CursorLeft = 0;

            try
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;

                Console.CursorLeft = 0;

                const int MAX_LEN = 40;

                if ((objectName != null) && (objectName.Length > MAX_LEN))
                    objectName = "..\\" + objectName.Substring(objectName.Length - MAX_LEN, MAX_LEN).Replace("/", "\\");

                int percent = (progress * 100) / total;
                string text = $"[{percent}% files]";
                string detailed = $"{text} {objectName}";

                Console.Write(detailed);
                Console.Title = $"{_title} [{percent}%]";

                // This is ensure that any previous printed text will be overwritten.
                Console.ForegroundColor = ConsoleColor.White;

                int pad = 70 - text.Length;
                Console.Write("".PadRight(pad, ' '));

                _logger.LastPrintedStats = true;
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public void OnCompleted(int synchronizedCount, int ignoredCount, int errorCount, int deletedCount,
            int fileScanned, int fileSynchronized, long readSize, long writeSize)
        {
            _logger.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            _logger.WriteLine($"Terminated: {synchronizedCount} synchronized, {ignoredCount} ignored, {errorCount} errors, {deletedCount} deleted");
            _logger.WriteLine($"{fileScanned} files scanned, {fileSynchronized} files synchronized, {Helpers.FormatByteSize(readSize)} read, {Helpers.FormatByteSize(writeSize)} written");
            Console.ForegroundColor = ConsoleColor.White;
        }
        #endregion

        #region Information methods
        private void WriteComponentLabels(IRepositorySource source)
        {
            string transformLabel = source.Transform != null ? source.Transform.Description : "none";

            Console.ForegroundColor = ConsoleColor.DarkYellow;

            _logger.WriteLine($"Source: {source.Description}");
            _logger.WriteLine($"Destination: {source.Destination.Description}");
            _logger.WriteLine($"Transform: {transformLabel}");
            _logger.WriteLine($"Date: {DateTime.Now.ToString()}");
            _logger.WriteLine($"Computer: {Environment.MachineName}");
            _logger.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
        }

        private void DisplayHeader()
        {
            Console.Clear();

            _logger.WriteLine($"{_title} - Jimmy Bourque (GNU General Public License)");
            _logger.WriteLine("--------------------------------------------------------");
            _logger.WriteLine();
        }

        private void DisplayFooter()
        {
            _logger.WriteLine();
            _logger.WriteLine("--------------------------------------------------------");
            _logger.WriteLine();
        }

        public void Dispose()
        {
            lock (this)
                if (!_disposed)
                {
                    _logger.Dispose();
                    _disposed = true;
                }
        }
        #endregion
    }
}
