using StorageManagementKit.Core.Crypto;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Repositories;
using System;

namespace StorageManagementKit.Core.Transforms
{
    public class UnsecureTransform : ITransforming
    {
        #region Members
        private readonly TripleDES _3des;
        #endregion

        #region Propertes
        public ILogging Logger { get; set; }
        public bool IsSecured { get; set; } = false;
        public string Description { get { return $"Triple-DES decryption"; } }
        public byte[] Key { get; private set; }
        public byte[] IV { get; private set; }
        #endregion

        #region Constructors
        /// <param name="key">3-DES key</param>
        /// <param name="iv">3-DES vector IV</param>
        public UnsecureTransform(byte[] key, byte[] iv, ILogging logger)
        {
            Logger = logger ?? throw new ArgumentNullException("logger");
            Key = key ?? throw new ArgumentNullException("key");
            IV = iv ?? throw new ArgumentNullException("iv");

            _3des = new TripleDES(key, iv);
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Secures the file object with a 3-DES algorithm
        /// </summary>
        public FileObject Process(FileObject fo)
        {
            FileObject file = new FileObject()
            {
                MetadataMD5 = fo.MetadataMD5,
                IsSecured = false
            };

            try
            {
                file.DataContent = _3des.Decrypt(fo.DataContent);
            }
            catch (Exception ex)
            {
                throw new JboBackupException(
                    string.Format(ErrorResources.UnsecureTransform_ProcessException, "DataContent", fo.Metadata.FullName), ex);
            }

            try
            {
                file.MetadataContent = _3des.Decrypt(fo.MetadataContent);
            }
            catch (Exception ex)
            {
                throw new JboBackupException(
                    string.Format(ErrorResources.UnsecureTransform_ProcessException, "MetadataContent", fo.Metadata.FullName), ex);
            }

            // Deserialize the metadata
            file.Metadata = file.MetadataContent.XmlToObject<FileMetadata>();

            // Be sure the metadata is authentic
            if (file.MetadataMD5 != MD5.CreateHash(file.Metadata.ToBytes()))
            {
                Logger.WriteLog(ErrorCodes.UnsecureTransform_InvalidMetadataSignature,
                    string.Format(ErrorResources.UnsecureTransform_InvalidMetadataSignature, fo.Metadata.FullName),
                    Severity.Warning, VerboseLevel.User);
                return null;
            }

            // Be sure the original file is authentic
            if (file.Metadata.OriginalMD5 != MD5.CreateHash(file.DataContent))
            {
                Logger.WriteLog(ErrorCodes.UnsecureTransform_InvalidMetadataSignature,
                    string.Format(ErrorResources.UnsecureTransform_InvalidDataContentSignature, fo.Metadata.FullName),
                    Severity.Warning, VerboseLevel.User);
                return null;
            }

            return file;
        }
        #endregion  
    }
}
