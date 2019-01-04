namespace StorageManagementKit.Core.Repositories
{
    public class FileObject
    {
        /// <summary>
        /// True = data are encrypted
        /// </summary>
        public bool IsSecured { get; set; }

        /// <summary>
        /// Contains the crypted file
        /// </summary>
        public byte[] DataContent { get; set; }

        /// <summary>
        /// Metadata contains the MD5 signature of the original decrypted data from the CryptedData property 
        /// </summary>
        public FileMetadata Metadata { get; set; }

        /// <summary>
        /// Serialized metadata
        /// </summary>
        public string MetadataContent { get; set; }

        /// <summary>
        /// This MD5 is the signature of metadata
        /// </summary>
        public string MetadataMD5 { get; set; }
    }
}
