using CosmosKit;
using Famoria.Domain.Converters;
using Famoria.Domain.Entities;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

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
        var databaseId = builder.Configuration["BlobContainerSettings:ConnectionString"]!;
        builder.Services.AddCosmosKit(databaseId,
        [
            new EntityContainer(typeof(Family), "families", nameof(Family.Id)),
            new EntityContainer(typeof(FamilyItem), "family-items", nameof(FamilyItem.FamilyId)),
            new EntityContainer(typeof(FamilyTask), "family-tasks", nameof(FamilyTask.FamilyId)),
            new EntityContainer(typeof(FamoriaUser), "users", nameof(FamoriaUser.Id)),
        ], options =>
        {
            options.TypeInfoResolver = FamoriaJsonContext.Default;
            options.Converters.Add(new FamilyItemPayloadConverter());
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        builder.AddAzureCosmosClient("cosmos",
            cosmosSettings =>
            {
                cosmosSettings.DisableTracing = false;
                //cosmosSettings.ConnectionString = settings.ConnectionString;
            },
            clientOptions =>
            {
                clientOptions.ApplicationName = AppDomain.CurrentDomain.FriendlyName;
                clientOptions.CosmosClientTelemetryOptions = new() { DisableDistributedTracing = false };
            });

        builder.Services
            .AddOptions<CosmosClientOptions>("cosmos")
            .Configure<CosmosSerializer>((opts, serializer) =>
            {
                opts.Serializer = serializer;
            });

        return builder;
    }

    public static IHostApplicationBuilder AddBlobContainer(this IHostApplicationBuilder builder)
    {
        builder.AddAzureBlobContainerClient("blob-container",
            blobsSettings =>
            {
                blobsSettings.DisableTracing = false;
                blobsSettings.ConnectionString = builder.Configuration["BlobContainerSettings:ConnectionString"];
            });

        return builder;
    }
}
