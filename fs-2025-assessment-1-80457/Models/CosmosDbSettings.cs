namespace fs_2025_assessment_1_80457.Models
{
    // Model to hold Cosmos DB connection and resource configuration settings.
    public class CosmosDbSettings
    {
        public string EndpointUri { get; set; } = string.Empty;
        public string PrimaryKey { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
    }
}