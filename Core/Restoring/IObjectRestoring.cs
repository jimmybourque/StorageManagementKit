namespace StorageManagementKit.Core.Restoring
{
    public interface IObjectRestoring
    {
        ObjectVersion[] GetVersions(string filename);
        bool Restore(ObjectVersion version, ref string destination);
    }
}