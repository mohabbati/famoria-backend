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
cosmosDb.AddContainer("users", "/id");

var blobContainer = builder.AddAzureStorage("blob-container").RunAsEmulator(x =>
    {
        x.WithLifetime(ContainerLifetime.Persistent);
    })
    .AddBlobs("blobs")
    .AddBlobContainer("famoria");

#region Dev Mail
//// GreenMail container (IMAP/SMTP)
//var mail = builder
//    .AddContainer("dev-mail",               // logical name in Aspire
//                  "greenmail/standalone",   // image
//                  "2.1.3")                  // tag
//                                            // one test user + all protocols (SMTP/IMAP/POP3)
//    .WithEnvironment("GREENMAIL_OPTS", "-Dgreenmail.setup.test.all -Dgreenmail.users=test1:pwd1")
//    .WithEndpoint(port: 1143, targetPort: 3143, name: "imap")
//    .WithEndpoint(port: 1025, targetPort: 3025, name: "smtp")
//    .WithLifetime(ContainerLifetime.Persistent);

//// Roundcube web-mail
//builder.AddContainer("webmail", "roundcube/roundcubemail", "1.6.10-apache")
//       // IMAP host & port
//       .WithEnvironment("ROUNDCUBEMAIL_DEFAULT_HOST", mail.GetEndpoint("imap"))
//       .WithEnvironment("ROUNDCUBEMAIL_DEFAULT_PORT", "1143")
//       // SMTP host & port
//       .WithEnvironment("ROUNDCUBEMAIL_SMTP_SERVER", mail.GetEndpoint("smtp"))
//       .WithEnvironment("ROUNDCUBEMAIL_SMTP_PORT", "1025")
//       // expose the web UI
//       .WithEndpoint(8088, 80, "http");   // http://localhost:8088
#endregion

var api = builder.AddProject<Projects.Famoria_Api>("famoria-api")
    .WithReference(cosmos)
    .WaitFor(cosmosDb);
var apiProxy = builder.AddProject<Projects.Famoria_Api_Proxy>("famoria-api-proxy")
    .WaitFor(api);
builder.AddProject<Projects.Famoria_AuthTester>("famoria-authtester")
    .WithReference(apiProxy);
builder.AddProject<Projects.Famoria_Email_Fetcher_Worker>("famoria-email-fetcher-worker")
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
