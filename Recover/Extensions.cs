using StorageManagementKit.Core;
using StorageManagementKit.Core.Restoring;
using System;

namespace StorageManagementKit.Recover
{
    public static class Extensions
    {
        public static RestoringRepositorySource ConvertToSourceRepository(this string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception("Missing source");

            if (value.ToLower() == RestoringRepositorySource.GCS.ToString().ToLower())
                return RestoringRepositorySource.GCS;
            else if (value.ToLower() == RestoringRepositorySource.S3.ToString().ToLower())
                return RestoringRepositorySource.S3;

            throw new SmkException($"Supported sources are [{RestoringRepositorySource.GCS}|{RestoringRepositorySource.S3}]");
        }
    }
}
