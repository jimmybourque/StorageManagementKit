namespace StorageManagementKit.Types
{
    public static class Constants
    {
        // Storage object names
        public const string EncryptedExt = ".encrypted";
        public const string MetadataExt = ".meta";
        public const string MD5Ext = ".md5";
        public const string Hive = ".smk-hive";
        public const string Bin = "_smk-bin";

        // Storage attributes
        public const string MetadataEncryptedKey = "Metadata.encrypted";
        public const string MetadataMD5Key = "MetadataMD5";
        public const string OriginalMD5Key = "OriginalMD5";
    }
}
