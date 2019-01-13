namespace StorageManagementKit.Core.Copying.Destinations
{
    public enum DestinationRepository
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
}
