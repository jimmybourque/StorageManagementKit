namespace StorageManagementKit.Core.Restoring
{
    public class RestoringSettings
    {
        public RestoringRepositorySource Repository { get; set; } = RestoringRepositorySource.None;
        public string Path { get; set; }
        public string ApiKey { get; set; }
        public string CryptoKey { get; set; }
    }
}
