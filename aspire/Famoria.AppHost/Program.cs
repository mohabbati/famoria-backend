#pragma warning disable ASPIRECOSMOSDB001

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator(x =>
    {
        x.WithGatewayPort(8081);
        x.WithLifetime(ContainerLifetime.Persistent);
        x.WithUrl("https://localhost:8081/_explorer/index.html", "Data Explorer UI");
    })
    .PublishAsConnectionString();
var cosmosDb = cosmos.AddCosmosDatabase("cosmos-db", "famoria");
cosmosDb.AddContainer("users", "/id");
cosmosDb.AddContainer("families", "/id");
cosmosDb.AddContainer("user-linked-accounts", "/provider");
cosmosDb.AddContainer("family-items", "/familyId");
cosmosDb.AddContainer("family-tasks", "/familyId");

var storageAccount = builder.AddAzureStorage("storage-account").RunAsEmulator(x =>
    {
        x.WithLifetime(ContainerLifetime.Persistent);
        x.WithDataVolume("famoria-blob-storage");
        x.WithBlobPort(10000);
    });
var blobs = storageAccount.AddBlobs("blobs");
var blobContainer = blobs.AddBlobContainer("blob-container", "famoria");

var api = builder.AddProject<Projects.Famoria_Api>("famoria-api")
    .WithEnvironment("CosmosDbSettings:AccountEndpoint", "https://a.valid.url/")
    .WithEnvironment("CosmosDbSettings:DatabaseId", "famoria")
    .WithEnvironment("BlobContainerSettings:ServiceUri", "https://a.valid.url/")
    .WithEnvironment("BlobContainerSettings:ContainerName", "famoria")
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithReference(blobs)
    .WaitFor(blobs);
builder.AddProject<Projects.Famoria_Email_Fetcher_Worker>("famoria-email-fetcher-worker")
    .WithEnvironment("CosmosDbSettings:AccountEndpoint", "https://a.valid.url/")
    .WithEnvironment("CosmosDbSettings:DatabaseId", "famoria")
    .WithEnvironment("BlobContainerSettings:ServiceUri", "https://a.valid.url/")
    .WithEnvironment("BlobContainerSettings:ContainerName", "famoria")
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithReference(blobs)
    .WaitFor(blobs);
//builder.AddProject<Projects.Famoria_Email_Filter_Worker>("famoria-email-filter-worker")
//    .WithReference(cosmos)
//    .WaitFor(cosmos)
//    .WithReference(blobContainer)
//    .WaitFor(blobContainer);
//builder.AddProject<Projects.Famoria_Tasker_Worker>("famoria-tasker-worker")
//    .WithReference(cosmos)
//    .WaitFor(cosmos)
//    .WithReference(blobContainer)
//    .WaitFor(blobContainer);
//builder.AddProject<Projects.Famoria_Summarizer_Worker>("famoria-summarizer-worker")
//    .WithReference(cosmos)
//    .WaitFor(cosmos)
//    .WithReference(blobContainer)
//    .WaitFor(blobContainer);

builder.Build().Run();
