using System.IO;

namespace StorageManagementKit.Core.IO
{
    public static class Extensions
    {
        public static DiscoveredObject ToDiscoveredObject(this FileInfo fi)
        {
            return new DiscoveredObject()
            {
                DirectoryName = fi.DirectoryName,
                FullName = fi.FullName,
                Kind = ObjectKind.File
            };
        }

        public static DiscoveredObject ToDiscoveredObject(this DirectoryInfo fi)
        {
            return new DiscoveredObject()
            {
                DirectoryName = fi.FullName,
                FullName = fi.FullName,
                Kind = ObjectKind.Directory
            };
        }
    }
}
