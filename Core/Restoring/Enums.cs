namespace StorageManagementKit.Core.Restoring
{
    public enum RestoringRepositorySource
    {
        None = 0,
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
}
