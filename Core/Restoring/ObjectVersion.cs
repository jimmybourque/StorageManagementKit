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
        public object VersionId { get; set; }
        public object ObjectData { get; set; }

        /// <summary>
        /// The user must confirm these questions before to restore the file.
        /// </summary>
        public string[] Questions { get; set; }
    }
}
