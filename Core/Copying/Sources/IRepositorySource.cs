using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Copying.Destinations;
using StorageManagementKit.Core.Transforms;

namespace StorageManagementKit.Core.Copying.Sources
{
    public interface IRepositorySource
    {
        IRepositoryDestination Destination { get; set; }
        ITransforming Transform { get; set; }
        ILogging Logger { get; set; }
        string Description { get; }
        bool Process();
    }
}
