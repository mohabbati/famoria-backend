using Azure.Identity;
using Famoria.Application.Interfaces;
using Famoria.Domain.Entities;
using Famoria.Infrastructure.Persistence;
using Famoria.Infrastructure.Persistence.JsonSerialization;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Famoria.Infrastructure;

public static class ConfigureInfrastructure
{
    public static IHostApplicationBuilder AddApiInfra(this IHostApplicationBuilder builder)
    {
        builder.AddCosmosDb();
        builder.AddBlobContainer();

        builder.Services.AddScoped<CosmosLinqQuery>();
        builder.Services.AddScoped(typeof(ICosmosRepository<>), typeof(CosmosRepository<>));

        return builder;
    }

    public static IHostApplicationBuilder AddEmailFetcherInfra(this IHostApplicationBuilder builder)
    {
        builder.AddCosmosDb();
        builder.AddBlobContainer();

        builder.Services.AddScoped<CosmosLinqQuery>();
        builder.Services.AddScoped(typeof(ICosmosRepository<>), typeof(CosmosRepository<>));

        return builder;
    }

    public static IHostApplicationBuilder AddSummarizerInfra(this IHostApplicationBuilder builder)
    {
        builder.AddCosmosDb();
        builder.AddBlobContainer();

        builder.Services.AddSingleton<CosmosLinqQuery>();
        builder.Services.AddSingleton(typeof(ICosmosRepository<>), typeof(CosmosRepository<>));

        return builder;
    }

    public static IHostApplicationBuilder AddCosmosDb(this IHostApplicationBuilder builder)
    {
        RepositoryHelper.DatabaseId = builder.Configuration["CosmosDbSettings:DatabaseId"]!;

        builder.Services.AddSingleton(new ContainerResolver()
        {
            RegisteredContainers = new Dictionary<Type, string>()
            {
                { typeof(FamoriaUser), "users" },
                { typeof(Family), "families" },
                { typeof(UserLinkedAccount), "user-linked-accounts" },
                { typeof(FamilyItem), "family-items" },
                { typeof(FamilyItemAudit), "family-items-audits" }
            },
            RegisteredPartitionKeys = new Dictionary<Type, PropertyInfo>()
            {
                { typeof(FamoriaUser), typeof(FamoriaUser).GetProperty(nameof(FamoriaUser.Id))! },
                { typeof(Family), typeof(Family).GetProperty(nameof(Family.Id))! },
                { typeof(UserLinkedAccount), typeof(UserLinkedAccount).GetProperty(nameof(UserLinkedAccount.Provider))! },
                { typeof(FamilyItem), typeof(FamilyItem).GetProperty(nameof(FamilyItem.FamilyId))! },
                { typeof(FamilyItemAudit), typeof(FamilyItemAudit).GetProperty(nameof(FamilyItem.Id))! }
            }
        });

        builder.AddAzureCosmosClient("cosmos",
            cosmosSettings =>
            {
                cosmosSettings.DisableTracing = false;
                cosmosSettings.Credential = new WorkloadIdentityCredential();
                cosmosSettings.AccountEndpoint = new Uri(builder.Configuration["CosmosDbSettings:AccountEndpoint"]!);
            },
            clientOptions =>
            {
                clientOptions.EnableContentResponseOnWrite = false;
                clientOptions.ApplicationName = AppDomain.CurrentDomain.FriendlyName;
                clientOptions.CosmosClientTelemetryOptions = new() { DisableDistributedTracing = false };

                var jsonOptions = new JsonSerializerOptions
                {
                    TypeInfoResolver = FamoriaJsonContext.Default,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                jsonOptions.Converters.Add(new FamilyItemPayloadConverter());
                jsonOptions.Converters.Add(new JsonStringEnumConverter());
                clientOptions.Serializer = new CosmosSystemTextJsonSerializer(jsonOptions);
            });

        return builder;
    }

    private static IHostApplicationBuilder AddBlobContainer(this IHostApplicationBuilder builder)
    {
        builder.AddAzureBlobContainerClient("blobs",
            blobsSettings =>
            {
                blobsSettings.DisableTracing = false;
                blobsSettings.Credential = new WorkloadIdentityCredential();
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
