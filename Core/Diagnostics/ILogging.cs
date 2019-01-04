namespace StorageManagementKit.Core.Diagnostics
{
    public enum Severity
    {
        Information = 0,
        Warning = 1,
        Error = 2
    }

    public interface ILogging
    {
        string LogFile { get; }
        void WriteLog(int code, string message, Severity severity, VerboseLevel level, bool highlight = false);
        void WriteLine();
        void WriteLine(string text);
        void Write(string text);
    }
}
