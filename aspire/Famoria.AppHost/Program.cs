#pragma warning disable ASPIRECOSMOSDB001

var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator(x =>
    {
        x.WithLifetime(ContainerLifetime.Persistent);
        x.WithUrlForEndpoint("https", u =>
        {
            x.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var annotations);
            var httpsAnnotation = annotations!.First(z => z.Name == "https");
            u.Url = $"{httpsAnnotation.UriScheme}://{httpsAnnotation.TargetHost}:{httpsAnnotation.TargetPort}/_explorer/index.html";
            u.DisplayText = "Data Explorer UI";
        });
    })
    .PublishAsConnectionString();
var cosmosDb = cosmos.AddCosmosDatabase("cosmos-db", "famoria");
cosmosDb.AddContainer("families", "/id");
cosmosDb.AddContainer("family-items", "/FamilyId");
cosmosDb.AddContainer("family-tasks", "/FamilyId");

var blobContainer = builder.AddAzureStorage("blob-container").RunAsEmulator(x =>
    {
        x.WithLifetime(ContainerLifetime.Persistent);
    })
    .AddBlobs("blobs")
    .AddBlobContainer("famoria");

var mailpit = builder.AddMailPit("mail-pit").WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.Famoria_Api>("famoria-api")
    .WithReference(cosmos)
    .WaitFor(cosmosDb);
builder.AddProject<Projects.Famoria_Email_Fetcher_Worker>("famoria-email-fetcher-worker")
    .WithReference(mailpit)
    .WaitFor(mailpit)
    .WithReference(cosmos)
    .WaitFor(cosmosDb)
    .WithReference(blobContainer)
    .WaitFor(blobContainer);
builder.AddProject<Projects.Famoria_Email_Filter_Worker>("famoria-email-filter-worker")
    .WithReference(cosmos)
    .WaitFor(cosmosDb)
    .WithReference(blobContainer)
    .WaitFor(blobContainer);
builder.AddProject<Projects.Famoria_Tasker_Worker>("famoria-tasker-worker")
    .WithReference(cosmos)
    .WaitFor(cosmosDb)
    .WithReference(blobContainer)
    .WaitFor(blobContainer);
builder.AddProject<Projects.Famoria_Summarizer_Worker>("famoria-summarizer-worker")
    .WithReference(cosmos)
    .WaitFor(cosmosDb)
    .WithReference(blobContainer)
    .WaitFor(blobContainer);

builder.Build().Run();
