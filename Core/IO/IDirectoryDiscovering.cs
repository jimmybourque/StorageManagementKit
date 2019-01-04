using System.IO;

namespace StorageManagementKit.Core.IO
{
    public interface IDirectoryDiscovering
    {
        bool OnFileFound(FileInfo fi);
        void ScanProgress(int progress, int total, string objectName = null);
    }
}
