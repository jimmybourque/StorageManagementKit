using StorageManagementKit.Core;
using StorageManagementKit.Diagnostics.Logging;
using StorageManagementKit.Security.Crypto;
using StorageManagementKit.Types;
using System;
using System.Linq;

namespace SmkKeyGen
{
    public class Engine : IDisposable
    {
        #region Members
        private string[] _arguments;
        private ILogging _logger;
        private bool _disposed = false;
        private const string _title = "SMK-KeyGen";
        #endregion

        #region Constructors
        public Engine(string[] args)
        {
            _arguments = args ?? throw new ArgumentNullException("args");

            Console.Title = _title;
            
            var verboseLevel = _arguments.Any(a => a.ToLower() == $"/{Arguments.Debug}") ? VerboseLevel.Debug : VerboseLevel.User;
            _logger = new ConsoleLogger(verboseLevel);
        }

        ~Engine()
        {
            Dispose();
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

            // Generate a new key
            else
            {
                DisplayHeader();
                string value = ConsoleHelpers.GetCommandArgValue(_arguments, Arguments.File);
                TripleDES.GenerateKey(value, _logger);
                _logger.WriteLine(string.Format(ErrorResources.Engine_KeyFileGenerated, value));
                DisplayFooter();
            }
        }

        /// <summary>
        /// Display the help reference to the user
        /// </summary>
        private void DisplayHelp()
        {
            DisplayHeader();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Crypto");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.File}\t\tThe path of the new key file");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Examples");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Generate a new 3-DES crypto key");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"/{Arguments.File}=myfolder\\mykey.key");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
        }

        #region Information methods
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
        #endregion

        public void Dispose()
        {
            lock (this)
                if (!_disposed)
                {
                    _logger.Dispose();
                    _disposed = true;
                }
        }
    }
}
