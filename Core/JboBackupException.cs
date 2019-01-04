using System;

namespace StorageManagementKit.Core
{
    [Serializable]
    public class JboBackupException : Exception
    {
        public JboBackupException() { }
        public JboBackupException(string message) : base(message) { }
        public JboBackupException(string message, Exception inner) : base(message, inner) { }
        protected JboBackupException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
