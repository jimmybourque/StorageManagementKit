using System;

namespace StorageManagementKit.Diagnostics.Logging
{
    public enum Severity
    {
        Information = 0,
        Warning = 1,
        Error = 2
    }

    public interface ILogging : IDisposable
    {
        string LogFile { get; }
        bool LastPrintedStats { get; set; }
        void WriteLog(int code, string message, Severity severity, VerboseLevel level, bool highlight = false);
        void WriteLine();
        void WriteLine(string text);
        void Write(string text);
        void WriteError(string text);
    }
}
