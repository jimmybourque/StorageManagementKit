namespace StorageManagementKit.Core
{
    public interface IProgressing
    {
        void OnProgress(int progress, int total, string objectName = null);
        void OnCompleted(int synchronizedCount, int ignoredCount, int errorCount, int deletedCount,
            int fileScanned, int fileSynchronized, long readSize, long writeSize);
    }
}
