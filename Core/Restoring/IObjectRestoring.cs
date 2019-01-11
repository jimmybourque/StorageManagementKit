namespace StorageManagementKit.Core.Restoring
{
    public interface IObjectRestoring
    {
        string BucketName { get; }

        ObjectVersion[] GetVersions(string filename);

        bool Restore(ObjectVersion version, ref string destination);
    }
}