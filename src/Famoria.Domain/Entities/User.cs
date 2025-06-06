namespace Famoria.Domain.Entities;

public class User : AuditableEntity
{
    public required string Email { get; set; }
    public required string Provider { get; set; }
    public required string ExternalSub { get; set; }
    public required List<string> FamilyIds { get; set; } = [];
}
