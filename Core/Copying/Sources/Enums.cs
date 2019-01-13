namespace StorageManagementKit.Core.Copying.Sources
{
    public enum SourceRepository
    {
        Local,
        /// <summary>
        /// Google Cloud Storage
        /// </summary>
        GCS,
        /// <summary>
        /// Amazon S3
        /// </summary>
        S3,
        /// <summary>
        /// Azure Blob Storage
        /// </summary>
        ABS
    }

    public enum CheckLevel
    {
        LocalMD5,
        RemoteMD5,
        ArchiveFlag
    }
}
