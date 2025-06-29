using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Famoria.Infrastructure.Persistence;

internal class CosmosLinqQuery 
{
    public virtual FeedIterator<T> GetFeedIterator<T>(IQueryable<T> query)
    {
        return query.ToFeedIterator();
    }
}
