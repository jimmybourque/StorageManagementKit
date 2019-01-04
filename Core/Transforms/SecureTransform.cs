using StorageManagementKit.Core.Crypto;
using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Repositories;
using System;

namespace StorageManagementKit.Core.Transforms
{
    public class SecureTransform : ITransforming
    {
        #region Properties
        public ILogging Logger { get; set; }
        private readonly TripleDES _3des;
        public bool IsSecured { get; set; } = true;
        public string Description { get { return $"Triple-DES encryption"; } }
        public byte[] Key { get; private set; }
        public byte[] IV { get; private set; }
        #endregion

        #region Constructors
        /// <param name="key">3-DES key</param>
        /// <param name="iv">3-DES vector IV</param>
        public SecureTransform(byte[] key, byte[] iv, ILogging logger)
        {
            Logger = logger ?? throw new ArgumentNullException("logger");

            if (key == null) throw new ArgumentNullException("key");
            if (iv == null) throw new ArgumentNullException("iv");

            Key = key;
            IV = iv;
            _3des = new TripleDES(key, iv);
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Secures the file object with a 3-DES algorithm
        /// </summary>
        public FileObject Process(FileObject fo)
        {
            try
            {
                return new FileObject()
                {
                    DataContent = _3des.Encrypt(fo.DataContent),
                    Metadata = new FileMetadata()
                    {
                        Attributes = fo.Metadata.Attributes,
                        LastWriteTime = fo.Metadata.LastWriteTime,
                        Name = fo.Metadata.Name,
                        FullName = fo.Metadata.FullName,
                        OriginalMD5 = fo.Metadata.OriginalMD5
                    },
                    MetadataContent = _3des.Encrypt(fo.MetadataContent),
                    MetadataMD5 = fo.MetadataMD5,
                    IsSecured = true
                };
            }
            catch (Exception ex)
            {
                throw new JboBackupException(string.Format(ErrorResources.SecureTransform_ProcessException, fo.Metadata.FullName), ex);
            }
        }
        #endregion
    }
}
