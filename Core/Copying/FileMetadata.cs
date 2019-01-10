using System;
using System.IO;

namespace StorageManagementKit.Core.Copying
{
    public class FileMetadata
    {
        public string Name { get; set; }

        public string FullName { get; set; }

        /// <summary>
        /// Signature of the original file data (decrypted version)
        /// </summary>
        public string OriginalMD5 { get; set; }

        private DateTime _lastWriteTime;
        public DateTime LastWriteTime
        {
            get { return _lastWriteTime; }
            set
            {
                _lastWriteTime = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond, DateTimeKind.Local);
            }
        }

        public FileAttributes Attributes { get; set; }
    }
}
