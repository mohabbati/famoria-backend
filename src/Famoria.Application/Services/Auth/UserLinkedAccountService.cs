using System.Threading;
using System.Threading.Tasks;

using Famoria.Application.Interfaces;
using Famoria.Domain.Entities;

using Microsoft.Azure.Cosmos;

namespace Famoria.Application.Services;

public class UserLinkedAccountService : IUserLinkedAccountService
{
    private readonly Container _container;

    public UserLinkedAccountService(CosmosClient cosmosClient)
    {
        // Assumes database name is 'FamoriaDb' and container is 'user-linked-accounts'
        _container = cosmosClient.GetDatabase("FamoriaDb").GetContainer("user-linked-accounts");
    }

    public async Task UpsertAsync(UserLinkedAccount connection, CancellationToken cancellationToken)
    {
        // Use FamilyId as the partition key
        await _container.UpsertItemAsync(connection, new PartitionKey(connection.FamilyId), cancellationToken: cancellationToken);
    }
}
