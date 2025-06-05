using System.Text.Json;
using Famoria.Application.Models;
using Famoria.Domain.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Famoria.Infrastructure;

public static class ConfigureInfrastructure
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder, AppSettings appSettings)
    {
        builder.Services.AddSingleton(appSettings.CosmosDbSettings);

        builder.AddCosmosDb(appSettings.CosmosDbSettings);
        builder.AddBlobContainer(appSettings.BlobContainerSettings);

        return builder;
    }

    public static IHostApplicationBuilder AddCosmosDb(this IHostApplicationBuilder builder, CosmosDbSettings settings)
    {
        builder.Services.Configure<JsonSerializerOptions>(options =>
        {
            options.Converters.Add(new FamilyItemPayloadConverter());
        });

        builder.AddAzureCosmosClient("cosmos",
            cosmosSettings =>
            {
                cosmosSettings.DisableTracing = false;
                cosmosSettings.ConnectionString = settings.ConnectionString;
            },
            clientOptions =>
            {
                clientOptions.Serializer = new CosmosSystemTextJsonSerializer(new JsonSerializerOptions
                {
                    TypeInfoResolver = FamoriaJsonContext.Default,
                    Converters = { new FamilyItemPayloadConverter() }
                });

                clientOptions.ApplicationName = AppDomain.CurrentDomain.FriendlyName;
                clientOptions.CosmosClientTelemetryOptions = new() { DisableDistributedTracing = false };
            });

        //builder.Services.AddSingleton(new ContainerResolver()
        //{
        //    RegisteredContainers = new Dictionary<Type, string>()
        //    {
        //        { typeof(Family), "families" }
        //    },
        //    RegisteredPartitionKeys = new Dictionary<Type, PropertyInfo>()
        //    {
        //        { typeof(Family), typeof(Family).GetProperty(nameof(Family.Id))! }
        //    }
        //});

        return builder;
    }

    public static IHostApplicationBuilder AddBlobContainer(this IHostApplicationBuilder builder, BlobContainerSettings settings)
    {
        builder.AddAzureBlobContainerClient("blob-container",
            blobsSettings =>
            {
                blobsSettings.DisableTracing = false;
                blobsSettings.ConnectionString = settings.ConnectionString;
            });

        return builder;
    }
}
