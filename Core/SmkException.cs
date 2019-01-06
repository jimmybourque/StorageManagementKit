using System;

namespace StorageManagementKit.Core
{
    [Serializable]
    public class SmkException : Exception
    {
        public SmkException() { }
        public SmkException(string message) : base(message) { }
        public SmkException(string message, Exception inner) : base(message, inner) { }
        protected SmkException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
