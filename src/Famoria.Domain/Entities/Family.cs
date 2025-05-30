namespace Famoria.Domain.Entities;

public class Family : AuditableEntity
{
    public required string DisplayName { get; set; }
    public required List<FamilyMember> Members { get; set; } = [];
}
