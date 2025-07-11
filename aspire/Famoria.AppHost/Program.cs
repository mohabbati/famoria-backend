#pragma warning disable ASPIRECOSMOSDB001

var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator(x =>
    {
        x.WithGatewayPort(8081);
        x.WithLifetime(ContainerLifetime.Persistent);
        x.WithUrl("https://localhost:8081/_explorer/index.html", "Data Explorer UI");
    });
var cosmosDb = cosmos.AddCosmosDatabase("cosmos-db", "famoria");
cosmosDb.AddContainer("users", "/id");
cosmosDb.AddContainer("families", "/id");
cosmosDb.AddContainer("user-linked-accounts", "/provider");
cosmosDb.AddContainer("family-items", "/familyId");
cosmosDb.AddContainer("family-items-leases", "/id");
cosmosDb.AddContainer("family-items-audits", "/id");

var storageAccount = builder.AddAzureStorage("storage-account").RunAsEmulator(x =>
    {
        x.WithLifetime(ContainerLifetime.Persistent);
        x.WithDataVolume("famoria-blob-storage");
        x.WithBlobPort(10000);
    });
var blobs = storageAccount.AddBlobs("blobs");
var blobContainer = blobs.AddBlobContainer("blob-container", "famoria");

var ollama = builder.AddOllama("ollama", 11434)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("ollama")
    .AddModel("llama3");

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
builder.AddProject<Projects.Famoria_Summarizer_Worker>("famoria-summarizer-worker")
    .WithEnvironment("CosmosDbSettings:AccountEndpoint", "https://a.valid.url/")
    .WithEnvironment("CosmosDbSettings:DatabaseId", "famoria")
    .WithEnvironment("BlobContainerSettings:ServiceUri", "https://a.valid.url/")
    .WithEnvironment("BlobContainerSettings:ContainerName", "famoria")
    .WithEnvironment("Ai:Endpoint", "http://localhost:11434/")
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithReference(ollama)
    .WaitFor(ollama);

//builder.AddProject<Projects.Famoria_AuthTester>("famoria-auth-tester")
//    .WaitFor(api);

builder.Build().Run();
