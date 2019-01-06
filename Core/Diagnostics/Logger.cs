using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StorageManagementKit.Core.Diagnostics
{
    public class Logger : ILogging, IDisposable
    {
        #region Members
        private StreamWriter _logWriter = null;
        private string _logfile;
        private int _age;
        private bool _disposed = false;
        #endregion

        #region Properties
        public VerboseLevel VerboseLevel { get; set; }
        public string LogFile { get; private set; }
        public bool LastPrintedStats { get; set; } = false;
        #endregion

        #region Constructor
        public Logger(string filename, int age, VerboseLevel level)
        {
            _logfile = filename;
            _age = age;
            VerboseLevel = level;

            InitLogFile();
            ExpireLogs();
        }

        ~Logger()
        {
            Dispose();
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Display an error in red
        /// </summary>
        public void WriteError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Write an empty line
        /// </summary>
        public void WriteLine()
        {
            ResetProgressDisplay();
            WriteLine("");
        }

        /// <summary>
        /// Method invoked by all private objects to report a log to the user
        /// </summary>
        public void WriteLog(int code, string message, Severity severity, VerboseLevel level, bool highlight = false)
        {
            if ((level != VerboseLevel.User) && (level != VerboseLevel))
                return;

            ConsoleColor bckColor = Console.ForegroundColor;

            try
            {
                switch (severity)
                {
                    case Severity.Information:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case Severity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case Severity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }

                string hlStart = highlight ? ">> " : "";
                string hlStop = highlight ? " <<" : "";

                string log = $"{DateTime.Now.ToString("HH:mm:ss")} [{code} {severity.ToString().Substring(0, 4)}] {hlStart}{message}{hlStop}";
                WriteLine(log);
            }
            finally
            {
                Console.ForegroundColor = bckColor;
            }
        }

        /// <summary>
        /// Write raw text to the logfile
        /// </summary>
        public void Write(string text)
        {
            lock (_logWriter)
            {
                _logWriter.Write(text);
                _logWriter.Flush();
            }

            ResetProgressDisplay();
            Console.Write(text);
        }

        /// <summary>
        /// Write raw text to the logfile
        /// </summary>
        public void WriteLine(string text)
        {
            lock (_logWriter)
            {
                _logWriter.WriteLine(text);
                _logWriter.Flush();
            }

            ResetProgressDisplay();
            Console.WriteLine(text);
        }
        #endregion

        #region Private methods
        private void ResetProgressDisplay()
        {
            if (LastPrintedStats)
            {
                Console.CursorLeft = 0;
                Console.Write(" ".PadRight(100, ' '));
                Console.CursorLeft = 0;
                LastPrintedStats = false;
            }
        }

        /// <summary>
        /// Initiates the handle of the log file
        /// </summary>
        private void InitLogFile()
        {
            // Creates the sub folder for logs
            string path = Path.GetDirectoryName(_logfile);

            if (!string.IsNullOrWhiteSpace(_logfile) && !string.IsNullOrEmpty(path))
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    throw new SmkException($"Failed while creating the path {path}", ex);
                }

            string newExt = $"{DateTime.Now.ToString("yyyyMMdd_HHmmss")}{Path.GetExtension(_logfile)}";

            LogFile = Path.GetFullPath(Path.ChangeExtension(_logfile, newExt));

            try
            {
                _logWriter = new StreamWriter(LogFile);
            }
            catch (Exception ex)
            {
                throw new SmkException($"Failed while creating the file {LogFile}", ex);
            }
        }

        /// <summary>
        /// Deletes all files older than age of [age] histories
        /// </summary>
        private void ExpireLogs()
        {
            string path = Path.GetDirectoryName(LogFile);

            if (string.IsNullOrEmpty(path))
                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string filename = Path.GetFileNameWithoutExtension(_logfile);
            string ext = Path.GetExtension(LogFile);

            string[] files = Directory.GetFiles(path, $"*{ext}", SearchOption.TopDirectoryOnly)
                .OrderBy(a => File.GetLastWriteTime(a))
                .ToArray();

            files = files.Where(f => f != LogFile).ToArray();

            if (files.Length > _age)
                for (int i = 5; i < files.Length; i++)
                {
                    try
                    {
                        File.Delete(files[i]);
                    }
                    catch (Exception ex)
                    {
                        throw new SmkException(string.Format(ErrorResources.LogHelpers_CleaningLogFailed, files[i]) + Environment.NewLine + ex.Message);
                    }
                }
        }

        public void Dispose()
        {
            lock (this)
                if (!_disposed)
                    try
                    {
                        _logWriter.Dispose();
                    }
                    finally
                    {
                        _disposed = true;
                    }
        }
        #endregion
    }
}
