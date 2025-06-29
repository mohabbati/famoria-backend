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
    });
var blobs = storageAccount.AddBlobs("blobs");
var blobContainer = blobs.AddBlobContainer("blob-container", "famoria");

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
    .WithEnvironment("CosmosDbSettings:AccountEndpoint", "https://a.valid.url/")
    .WithEnvironment("CosmosDbSettings:DatabaseId", "famoria")
    .WithEnvironment("BlobContainerSettings:ServiceUri", "https://a.valid.url/")
    .WithEnvironment("BlobContainerSettings:ContainerName", "famoria")
    .WithReference(cosmos)
    .WaitFor(cosmos);
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

//builder.AddProject<Projects.Famoria_AuthTester>("famoria-auth-tester")
//    .WaitFor(api);

builder.Build().Run();
