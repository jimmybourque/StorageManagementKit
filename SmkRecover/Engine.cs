using StorageManagementKit.Core;
using StorageManagementKit.Core.Restoring;
using StorageManagementKit.Diagnostics.Logging;
using StorageManagementKit.Types;
using System;
using System.Linq;

namespace SmkRecover
{
    public class Engine
    {
        #region Members
        private string[] _arguments;
        private ILogging _logger;
        private string _title = "SMK-Recover";
        #endregion

        #region Constructors
        public Engine(string[] args)
        {
            _arguments = args ?? throw new ArgumentNullException("args");

            Console.Title = _title;
            InitLogFile();
        }
        #endregion

        /// <summary>
        /// Take an action in according to the arguments
        /// </summary>
        public void Process()
        {
            // Display the help
            if (_arguments.Any(a => a.Equals("?") || a.Equals("-help")))
            {
                DisplayHelp();
                return;
            }

            else if (!ConsoleHelpers.AllCommandExist(_arguments, typeof(Arguments)))
                return;

            Recover();
        }

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

                _logger = new FileLogger(logfile, iage, verboseLevel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private void DisplayHelp()
        {
            DisplayHeader(false);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Source");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.Source}\t\tGoogle Cloud Storage [{RestoringRepositorySource.GCS}], Amazon [{RestoringRepositorySource.S3}] or Azure Blob Storage [{RestoringRepositorySource.ABS}]");
            Console.WriteLine($"/{Arguments.SourcePath}\tThe bucket name");
            Console.WriteLine($"/{Arguments.SourceApiKey}\tThe file that contains the API key. Used to decrypt the file");
            Console.WriteLine($"/{Arguments.SourceFile}\tThe bucket object key name; include the path with the filename");

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Destination");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.DestFile}\tThe file path where the file must be recovered");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Transformation");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.CryptoKey}\tThe file path that contains the key that will be used to decrypt the file");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Troubleshooting");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.Debug}\t\tDisplay all informations");
            Console.WriteLine($"/{Arguments.Log}\t\tThe log file name");
            Console.WriteLine($"/{Arguments.LogAge}\t\tThe number of log histories to keep (default=9)");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Recover a file from a cloud service bucket
        /// </summary>
        private void Recover()
        {
            Console.Clear();
            DisplayHeader(true);

            string source = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.Source);
            string sourceFile = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.SourceFile);
            string sourcePath = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.SourcePath);
            string sourceApiKey = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.SourceApiKey);
            string cryptoKey = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.CryptoKey);
            string destFile = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.DestFile);
            string debug = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.Debug);
            string log = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.Log);
            string logAge = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.LogAge);


            // Requests a filename from the user if it was not provided...
            if (string.IsNullOrEmpty(sourceFile))
            {
                Console.WriteLine(Resources.Engine_TypeFileName);
                sourceFile = Console.ReadLine().Replace("\\", "/");
            }
            else
                sourceFile = sourceFile.Replace("\\", "/");


            // Gets the list of versions about the requested object
            Console.Clear();
            DisplayHeader(false);
            _logger.WriteLine(Resources.Engine_ContactingService);
            _logger.WriteLine();

            RestoringSettings settings = new RestoringSettings()
            {
                Repository = source.ConvertToSourceRepository(),
                ApiKey = sourceApiKey,
                Path = sourcePath,
                CryptoKey = cryptoKey
            };

            // Creates the restoring instance for the selected cloud service source
            IObjectRestoring restorer = new ObjectRestoringFactory(_logger).Create(settings);

            // Requests the list of available versions for the given object
            ObjectVersion[] versions = restorer.GetVersions(sourceFile.ToLower());

            if (versions == null)
                return;

            DisplayList(restorer, versions, sourcePath, sourceFile);

            int choice = GetUserChoice(versions.Length);
            if (choice == -1)
                return;

            // If there is some questions to be confirmed by the user...
            if (!UserConfirmations(versions[choice - 1]))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Resources.Engine_ObjectNotRestored);
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            // The user has choosen the version, requests the file
            if (!restorer.Restore(versions[choice - 1], ref destFile))
                return;

            _logger.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            _logger.Write($"{Resources.Engine_RestoredFile} ");
            Console.ForegroundColor = ConsoleColor.White;
            _logger.WriteLine(destFile);

            DisplayFooter();
        }

        /// <summary>
        /// If there is some questions to be confirmed by the user...
        /// </summary>
        private static bool UserConfirmations(ObjectVersion version)
        {
            if (version.Questions != null)
                foreach (string question in version.Questions)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;

                    Console.WriteLine();
                    Console.Write(question);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" [y/N]: ");

                    Console.CursorVisible = true;
                    string response = Console.ReadLine();
                    Console.CursorVisible = false;

                    // If the user has answered No, stop now!
                    if (response == null)
                        return false;

                    if (!response.ToLower().Equals("y"))
                        return false;
                }

            return true;
        }

        /// <summary>
        /// Requests a choice to the user. He must select an object version.
        /// </summary>
        private static int GetUserChoice(int count)
        {
            string choice;
            int ichoice = -1;

            do
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"{Resources.Engine_EnterVersion} ");

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"[1-{count}]: ");

                Console.CursorVisible = true;
                choice = Console.ReadLine();
                Console.CursorVisible = false;

                // The user can type 'exist' to cancel the workflow
                if (choice.ToLower().Equals("exit"))
                    return -1;

                // Be sure the user has provided a valid numeric choice
                if (int.TryParse(choice, out ichoice) && (ichoice >= 1) && (ichoice <= count))
                    return ichoice;
            }
            while (true);
        }

        /// <summary>
        /// Displays the list of existing versions
        /// </summary>
        private void DisplayList(IObjectRestoring restorer, ObjectVersion[] versions, string bucket, string filename)
        {
            Console.Clear();
            DisplayHeader(false);

            Console.ForegroundColor = ConsoleColor.Gray;
            _logger.Write("Bucket: ");
            Console.ForegroundColor = ConsoleColor.White;
            _logger.WriteLine(restorer.BucketName);

            Console.ForegroundColor = ConsoleColor.Gray;
            _logger.Write("Object: ");

            Console.ForegroundColor = ConsoleColor.White;
            _logger.WriteLine(filename.Replace("\\", "/"));

            _logger.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            _logger.WriteLine("Choice\tTime created\t\tSize\tStorage\t\tGeneration");
            _logger.WriteLine("-------\t-----------------------\t-------\t--------------\t-----------------");

            // Enumerates each available version...
            Console.ForegroundColor = ConsoleColor.White;
            for (int i = 0; i < versions.Length; i++)
            {
                string size = Helpers.FormatByteSize(versions[i].Size);
                _logger.WriteLine($"[{i + 1}]\t{versions[i].TimeCreated}\t{size}\t{versions[i].StorageClass.PadRight(15, ' ')}\t{versions[i].VersionId}");
            }

            _logger.WriteLine();
        }

        private void DisplayHeader(bool writeLog)
        {
            Console.Clear();

            if (writeLog)
            {
                _logger.WriteLine($"{_title} - Jimmy Bourque (GNU General Public License)");
                _logger.WriteLine("--------------------------------------------------------");
                _logger.WriteLine();
            }
            else
            {
                Console.WriteLine($"{_title} - Jimmy Bourque (GNU General Public License)");
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine();
            }
        }

        private void DisplayFooter()
        {
            _logger.WriteLine();
            _logger.WriteLine("--------------------------------------------------------");
            _logger.WriteLine();
        }
    }
}
