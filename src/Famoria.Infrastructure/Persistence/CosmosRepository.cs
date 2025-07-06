using Famoria.Application.Interfaces;
using Famoria.Domain.Common;
using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;

namespace Famoria.Infrastructure.Persistence;

internal sealed class CosmosRepository<TEntity> : ICosmosRepository<TEntity>
    where TEntity : EntityBase
{
    private readonly Container _container;
    private readonly CosmosLinqQuery _cosmosLinqQuery;
    private readonly ContainerResolver _containerResolver;

    public Container Container => _container;

    public CosmosRepository(CosmosClient cosmosClient, ContainerResolver containerResolver, CosmosLinqQuery cosmosLinqQuery)
    {
        _container = cosmosClient.GetContainer(RepositoryHelper.DatabaseId, containerResolver.ResolveName(typeof(TEntity)));
        _cosmosLinqQuery = cosmosLinqQuery;
        _containerResolver = containerResolver;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        RepositoryHelper.SetEntityDefaults(entity);

        var partitionKeyValue = GetPartitionKeyValue(entity);

        var response = await _container.CreateItemAsync(entity, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, string partitionKeyValue, CancellationToken cancellationToken)
    {
        await _container.DeleteItemAsync<TEntity>(id, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
    }

    public async Task<TEntity?> GetByAsync(string id, string partitionKeyValue, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.ReadItemAsync<TEntity>(id, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate, string partitionKeyValue, CancellationToken cancellationToken)
    {
        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(partitionKeyValue)
        };

        var linqOptions = new CosmosLinqSerializerOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        };

        var queryable = _container.GetItemLinqQueryable<TEntity>(
            requestOptions: requestOptions,
            linqSerializerOptions: linqOptions).Where(predicate);

        var feedIterator = _cosmosLinqQuery.GetFeedIterator(queryable);

        var results = new List<TEntity>();

        while (feedIterator.HasMoreResults)
        {
            FeedResponse<TEntity> response = await feedIterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        RepositoryHelper.SetEntityDefaults(entity);

        var partitionKeyValue = GetPartitionKeyValue(entity);

        await _container.ReplaceItemAsync(entity, entity.Id, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
    }

    public async Task UpsertAsync(TEntity entity, CancellationToken cancellationToken)
    {
        RepositoryHelper.SetEntityDefaults(entity);

        var partitionKeyValue = GetPartitionKeyValue(entity);

        await _container.UpsertItemAsync(entity, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
    }

    private string GetPartitionKeyValue(TEntity entity)
    {
        var partitionKeyValue = _containerResolver.ResolvePartitionKey(typeof(TEntity))?.GetValue(entity)?.ToString();

        if (partitionKeyValue == null)
        {
            throw new ArgumentException($"The property '{typeof(TEntity)}' does not exist or is null.");
        }

        return partitionKeyValue;
    }
}
