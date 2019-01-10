using StorageManagementKit.Core;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Restoring;
using System;
using System.Linq;
using System.Reflection;

namespace StorageManagementKit.GcsRecover
{
    class Program
    {
        #region Members
        private static string[] _arguments;
        private static ILogging _logger;
        private static string _title = "SMK-Recover";
        #endregion

        static void Main(string[] args)
        {
            try
            {
                // Excludes the program name
                args = Environment.GetCommandLineArgs()
                   .Where(a => a.ToLower() != Assembly.GetExecutingAssembly().Location.ToLower())
                   .ToArray();

                _arguments = args ?? throw new ArgumentNullException("args");
                InitLogFile();

                Console.Title = _title;

                // Display the help
                if (_arguments.Any(a => a.Equals("?") || a.Equals("-help")))
                    DisplayHelp();

                else if (!ConsoleHelpers.AllCommandExist(_arguments, typeof(Arguments)))
                    return;

                Run();
            }
            catch (SmkException)
            {
                Console.WriteLine("Execution failed, see the log file");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Internal Error{Environment.NewLine}{ex.Message}");
            }
            finally
            {
#if DEBUG
                Console.WriteLine();
                Console.WriteLine($"Press a enter to exit");
                Console.ReadLine();
#endif
            }
        }

        private static void InitLogFile()
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

        private static void DisplayHelp()
        {
            throw new NotImplementedException();
        }

        private static void Run()
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
                Console.WriteLine("Type the file name to restore and press enter:");
                sourceFile = Console.ReadLine().Replace("\\", "/");
            }
            else
                sourceFile = sourceFile.Replace("\\", "/");


            // Gets the list of versions about the requested object
            Console.Clear();
            DisplayHeader(false);
            _logger.WriteLine("Contacting the cloud storage service...");
            _logger.WriteLine();

            RestoringSettings settings = new RestoringSettings()
            {
                Repository = source.ConvertToSourceRepository(),
                ApiKey = sourceApiKey,
                Path = sourcePath,
                CryptoKey = cryptoKey
            };

            // Creates the restoring instance for the selected cloud service source
            IRestoring restorer = new RestoringFactory(_logger).Create(settings);

            // Requests the list of available versions for the given object
            ObjectVersion[] versions = restorer.GetVersions(sourceFile.ToLower());

            if (versions == null)
                return;

            DisplayList(versions, sourcePath, sourceFile);

            int choice = GetUserChoice(versions.Length);
            if (choice == -1)
                return;

            // The user has choosen the version, requests the file
            if (!restorer.Restore(versions[choice - 1], ref destFile))
                return;

            _logger.Write($"{destFile} successfully restored");
            _logger.WriteLine();
        }

        /// <summary>
        /// Requests a choice to the user. He must select an object version.
        /// </summary>
        public static int GetUserChoice(int count)
        {
            string choice;
            int ichoice = -1;

            do
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("Enter the desired object version and press enter or type 'exit' to cancel ");

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"[1-{count}]: ");

                choice = Console.ReadLine();

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
        private static void DisplayList(ObjectVersion[] versions, string bucket, string filename)
        {
            Console.Clear();
            DisplayHeader(false);

            Console.ForegroundColor = ConsoleColor.Gray;
            _logger.Write("Object: ");

            Console.ForegroundColor = ConsoleColor.White;
            _logger.WriteLine($"gs://{bucket}/{filename.Replace("\\", " / ")}");
            _logger.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            _logger.WriteLine("Choice\tTime created\t\tSize\tStorage\t\tGeneration");
            _logger.WriteLine("-------\t-----------------------\t-------\t--------------\t-----------------");

            // Enumerates each available version...
            Console.ForegroundColor = ConsoleColor.White;
            for (int i = 0; i < versions.Length; i++)
            {
                string size = Helpers.FormatByteSize(versions[i].Size);
                _logger.WriteLine($"[{i + 1}]\t{versions[i].TimeCreated}\t{size}\t{versions[i].StorageClass}\t{versions[i].Generation}");
            }

            _logger.WriteLine();
        }

        private static void DisplayHeader(bool writeLog)
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
    }
}
