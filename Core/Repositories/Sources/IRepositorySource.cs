using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Repositories.Destinations;
using StorageManagementKit.Core.Transforms;

namespace StorageManagementKit.Core.Repositories.Sources
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
