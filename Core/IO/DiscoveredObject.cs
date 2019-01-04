using System.Diagnostics;

namespace StorageManagementKit.Core.IO
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
