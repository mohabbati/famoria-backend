namespace Famoria.Application.Models;

public class AppSettings
{
    public string CorsAllowedOrigins { get; set; } = default!;
    public CosmosDbSettings CosmosDbSettings { get; set; } = default!;
    public BlobContainerSettings BlobContainerSettings { get; set; } = default!;
}

public class CosmosDbSettings
{
    //public string ConnectionString { get; set; } = default!;
    public string DatabaseId { get; set; } = default!;
}

public class BlobContainerSettings
{
    public string ConnectionString { get; set; } = default!;
}
