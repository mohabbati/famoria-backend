using Microsoft.Azure.Cosmos;
using System.Linq.Expressions;

namespace Famoria.Application.Interfaces;

public interface ICosmosRepository<TEntity> where TEntity : EntityBase
{
    Container Container { get; }

    Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate, string partitionKeyValue, CancellationToken cancellationToken);

    Task<TEntity?> GetByAsync(string id, string partitionKeyValue, CancellationToken cancellationToken);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken);

    Task DeleteAsync(string id, string partitionKeyValue, CancellationToken cancellationToken);

    Task UpsertAsync(TEntity entity, CancellationToken cancellationToken);
}
