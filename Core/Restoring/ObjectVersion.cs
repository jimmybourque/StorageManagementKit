using System;
using System.Diagnostics;

namespace StorageManagementKit.Core.Restoring
{
    [DebuggerDisplay("{Name} [{TimeCreated}]")]
    public class ObjectVersion
    {
        public DateTime TimeCreated { get; set; }
        public string Name { get; set; }
        public string StorageClass { get; set; }
        public long Size { get; set; }
        public long? Generation { get; set; }
    }
}
