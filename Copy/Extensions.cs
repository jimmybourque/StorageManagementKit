using StorageManagementKit.Core.Repositories.Destinations;
using StorageManagementKit.Core.Repositories.Sources;
using StorageManagementKit.Core.Transforms;
using System;

namespace StorageManagementKit.Copy
{
    public static class Extensions
    {
        public static CheckLevel ConvertToCheckLevel(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return CheckLevel.LocalMD5; // Default value

            if (value.ToLower() == CheckLevel.LocalMD5.ToString().ToLower())
                return CheckLevel.LocalMD5;
            else if (value.ToLower() == CheckLevel.RemoteMD5.ToString().ToLower())
                return CheckLevel.RemoteMD5;
            else if (value.ToLower() == CheckLevel.ArchiveFlag.ToString().ToLower())
                return CheckLevel.ArchiveFlag;
            else
                throw new Exception($"Unsupported value for '/{Arguments.Check}={value}'");
        }

        public static SourceRepository ConvertToSourceRepository(this string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception("Missing source");

            if (value.ToLower() == SourceRepository.Local.ToString().ToLower())
                return SourceRepository.Local;
            else if (value.ToLower() == SourceRepository.GCS.ToString().ToLower())
                return SourceRepository.GCS;

            throw new Exception($"Supported sources are [{SourceRepository.Local}|{SourceRepository.GCS}]");
        }

        public static DestinationRepository ConvertToDestinationRepository(this string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception("Missing destination");

            if (value.ToLower() == DestinationRepository.Local.ToString().ToLower())
                return DestinationRepository.Local;
            else if (value.ToLower() == DestinationRepository.GCS.ToString().ToLower())
                return DestinationRepository.GCS;

            throw new Exception($"Supported destinations are [{DestinationRepository.Local}|{DestinationRepository.GCS}]");
        }

        public static TransformKind ConvertToTransformKind(this string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception("Missing transformation");

            if (value.ToLower() == TransformKind.Secure.ToString().ToLower())
                return TransformKind.Secure;
            else if (value.ToLower() == TransformKind.Unsecure.ToString().ToLower())
                return TransformKind.Unsecure;
            else if (value.ToLower() == TransformKind.None.ToString().ToLower())
                return TransformKind.None;

            throw new Exception($"Supported transformations are [{TransformKind.Secure}|{TransformKind.Unsecure}|{TransformKind.None}]");
        }
    }
}
