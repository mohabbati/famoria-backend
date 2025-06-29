using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using CosmosKit;
using Famoria.Domain.Converters;
using Famoria.Domain.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Famoria.Infrastructure;

public static class ConfigureInfrastructure
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddCosmosDb();
        builder.AddBlobContainer();

        return builder;
    }

    public static IHostApplicationBuilder AddCosmosDb(this IHostApplicationBuilder builder)
    {
        var databaseId = builder.Configuration["CosmosDbSettings:DatabaseId"]!;

        builder.Services.AddCosmosKit(databaseId,
        [
            new EntityContainer(typeof(FamoriaUser), "users", nameof(FamoriaUser.Id)),
            new EntityContainer(typeof(Family), "families", nameof(Family.Id)),
            new EntityContainer(typeof(UserLinkedAccount), "user-linked-accounts", nameof(UserLinkedAccount.Provider)),
            new EntityContainer(typeof(FamilyItem), "family-items", nameof(FamilyItem.FamilyId)),
            new EntityContainer(typeof(FamilyTask), "family-tasks", nameof(FamilyTask.FamilyId))
        ], options =>
        {
            options.TypeInfoResolver = FamoriaJsonContext.Default;
            options.Converters.Add(new FamilyItemPayloadConverter());
            options.Converters.Add(new JsonStringEnumConverter());
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        builder.AddAzureCosmosClient("cosmos",
            cosmosSettings =>
            {
                cosmosSettings.DisableTracing = false;
                cosmosSettings.Credential = new DefaultAzureCredential();
                cosmosSettings.AccountEndpoint = new Uri(builder.Configuration["CosmosDbSettings:AccountEndpoint"]!);
            },
            clientOptions =>
            {
                clientOptions.ApplicationName = AppDomain.CurrentDomain.FriendlyName;
                clientOptions.CosmosClientTelemetryOptions = new() { DisableDistributedTracing = false };
                clientOptions.Serializer = builder.Services.BuildServiceProvider().GetRequiredService<CosmosSerializer>();
            });

        return builder;
    }

    public static IHostApplicationBuilder AddBlobContainer(this IHostApplicationBuilder builder)
    {
        builder.AddAzureBlobContainerClient("blobs",
            blobsSettings =>
            {
                blobsSettings.DisableTracing = false;
                blobsSettings.Credential = new DefaultAzureCredential();
                blobsSettings.ServiceUri = new Uri(builder.Configuration["BlobContainerSettings:ServiceUri"]!);
                blobsSettings.BlobContainerName = builder.Configuration["BlobContainerSettings:ContainerName"];
            },
            clientBuilder =>
            {
                clientBuilder.ConfigureOptions(
                    options => options.Diagnostics.ApplicationId = "email-fetcher-worker");
            });

        return builder;
    }
}
