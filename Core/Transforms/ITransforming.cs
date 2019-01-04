using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Repositories;

namespace StorageManagementKit.Core.Transforms
{
    public interface ITransforming
    {
        ILogging Logger { get; set; }
        bool IsSecured { get; set; }
        byte[] Key { get; }
        byte[] IV { get; }
        string Description { get; }
        FileObject Process(FileObject fo);
    }
}
