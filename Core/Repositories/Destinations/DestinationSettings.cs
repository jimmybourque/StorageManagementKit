namespace StorageManagementKit.Core.Repositories.Destinations
{
    public class DestinationSettings
    {
        public DestinationRepository Repository { get; set; } = DestinationRepository.Local;
        public string Path { get; set; }
        public string OAuthFile { get; set; }
    }
}
