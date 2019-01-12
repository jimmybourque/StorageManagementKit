using System;

namespace StorageManagementKit.Security.Crypto
{
    [Serializable]
    public class SmkCryptoException : Exception
    {
        public SmkCryptoException() { }
        public SmkCryptoException(string message) : base(message) { }
        public SmkCryptoException(string message, Exception inner) : base(message, inner) { }
        protected SmkCryptoException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
