namespace StorageManagementKit.Core.Restoring
{
    public interface IRestoring
    {
        ObjectVersion[] GetVersions(string filename);
        bool Restore(ObjectVersion version, ref string destination);
    }
}