namespace StorageManagementKit.Core.Copying.Destinations
{
    public class DestinationSettings
    {
        public DestinationRepository Repository { get; set; } = DestinationRepository.Local;
        public string Path { get; set; }
        public string ApiKey { get; set; }
    }
}
