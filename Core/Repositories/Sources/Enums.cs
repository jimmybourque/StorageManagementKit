namespace StorageManagementKit.Core.Repositories.Sources
{
    public enum SourceRepository
    {
        Local,
        GCS
    }

    public enum CheckLevel
    {
        LocalMD5,
        RemoteMD5,
        ArchiveFlag
    }
}
