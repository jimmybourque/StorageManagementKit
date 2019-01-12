using System.Diagnostics;

namespace StorageManagementKit.IO.FileSystem
{
    public enum ObjectKind
    {
        File,
        Directory
    }

    [DebuggerDisplay("{FullName}")]
    public class DiscoveredObject
    {
        public string DirectoryName { get; set; }
        public string FullName { get; set; }
        public ObjectKind Kind { get; set; }
    }
}
