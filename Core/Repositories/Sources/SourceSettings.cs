namespace StorageManagementKit.Core.Repositories.Sources
{
    public class SourceSettings
    {
        public SourceRepository Repository { get; set; } = SourceRepository.Local;
        public string Path { get; set; }
        public CheckLevel CheckLevel { get; set; } = CheckLevel.LocalMD5;
        public bool NoCleaning { get; set; }
        public string ApiKey { get; set; }
        public bool WideDisplay { get; set; }
    }
}
