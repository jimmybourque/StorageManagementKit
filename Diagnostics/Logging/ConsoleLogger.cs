using System;

namespace StorageManagementKit.Diagnostics.Logging
{
    public class ConsoleLogger : ILogging, IDisposable
    {
        #region Properties
        public VerboseLevel VerboseLevel { get; set; }

        /// <summary>
        /// This property is not used but implement for ILogging
        /// </summary>
        string ILogging.LogFile { get; } = null;

        /// <summary>
        /// This property is not used but implement for ILogging
        /// </summary>
        bool ILogging.LastPrintedStats { get; set; } = false;

        public bool LastPrintedStats { get; set; } = false;
        #endregion

        #region Constructor
        public ConsoleLogger(VerboseLevel level)
        {
            VerboseLevel = level;
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
            ResetProgressDisplay();
            Console.Write(text);
        }

        /// <summary>
        /// Write raw text to the logfile
        /// </summary>
        public void WriteLine(string text)
        {
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
        /// Nothing to dispose, but implemented for ILogging
        /// </summary>
        public void Dispose()
        {

        }
        #endregion
    }
}
