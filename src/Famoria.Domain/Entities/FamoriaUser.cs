namespace Famoria.Domain.Entities;

public class FamoriaUser : AuditableEntity
{
    public required string Email { get; set; }
    public required string Provider { get; set; }
    public required string ExternalSub { get; set; }
    public required List<string> FamilyIds { get; set; } = [];
}
