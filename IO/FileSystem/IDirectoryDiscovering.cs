using System.IO;

namespace StorageManagementKit.IO.FileSystem
{
    public interface IDirectoryDiscovering
    {
        bool OnFileFound(FileInfo fi);
        void ScanProgress(int progress, int total, string objectName = null);
    }
}
