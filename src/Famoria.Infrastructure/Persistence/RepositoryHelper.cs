using Famoria.Domain.Common;

namespace Famoria.Infrastructure.Persistence;

internal class RepositoryHelper
{
    internal static string DatabaseId { get; set; } = string.Empty;

    internal static void SetEntityDefaults(EntityBase entity)
    {
        var utcNow = DateTime.UtcNow;
        var isNew = string.IsNullOrEmpty(entity.ETag);

        if (isNew && string.IsNullOrEmpty(entity.Id))
            entity.Id = Guid.NewGuid().ToString("N");

        if (entity is AuditableEntity auditableEntity)
        {
            if (isNew) auditableEntity.CreatedAt = utcNow;
        }
    }
}
