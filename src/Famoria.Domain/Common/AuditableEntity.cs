namespace Famoria.Domain.Common;

public abstract class AuditableEntity : EntityBase
{
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
