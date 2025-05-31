#pragma warning disable ASPIRECOSMOSDB001

var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos").RunAsPreviewEmulator();
var cosmosDb = cosmos.AddCosmosDatabase("famoria-db", "famoria");
cosmosDb.AddContainer("families", "/id");
cosmosDb.AddContainer("family-items", "/FamilyId");
cosmosDb.AddContainer("family-tasks", "/FamilyId");

var storage = builder.AddAzureStorage("storage").RunAsEmulator()
    .AddBlobs("blobs")
    .AddBlobContainer("emails");

builder.AddProject<Projects.Famoria_Api>("famoria-api")
    .WithReference(cosmos)
    .WaitFor(cosmosDb);
builder.AddProject<Projects.Famoria_Email_Fetcher_Worker>("famoria-email-fetcher-worker")
    .WithReference(cosmos)
    .WaitFor(cosmosDb)
    .WithReference(storage)
    .WaitFor(storage);
builder.AddProject<Projects.Famoria_Email_Filter_Worker>("famoria-email-filter-worker")
    .WithReference(cosmos)
    .WaitFor(cosmosDb)
    .WithReference(storage)
    .WaitFor(storage);
builder.AddProject<Projects.Famoria_Tasker_Worker>("famoria-tasker-worker")
    .WithReference(cosmos)
    .WaitFor(cosmosDb)
    .WithReference(storage)
    .WaitFor(storage);
builder.AddProject<Projects.Famoria_Summarizer_Worker>("famoria-summarizer-worker")
    .WithReference(cosmos)
    .WaitFor(cosmosDb)
    .WithReference(storage)
    .WaitFor(storage);


builder.Build().Run();
