using StorageManagementKit.Diagnostics.Logging;
using StorageManagementKit.IO.FileSystem;

namespace StorageManagementKit.Core.Copying.Destinations
{
    public interface IRepositoryDestination
    {
        ILogging Logger { get; set; }
        string Description { get; }
        bool Write(FileObject fo);
        bool IsMetadataMatch(string fullpath, bool isSecured, string sourceOriginalMd5);
        DiscoveredObject[] GetObjects();
        bool Delete(string file, bool wideDisplay);
        bool AfterDirectoryScan(string directory, bool wideDisplay);
    }
}
