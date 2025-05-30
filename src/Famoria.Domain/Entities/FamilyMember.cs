namespace Famoria.Domain.Entities;

public class FamilyMember
{
    public string? UserId { get; init; }
    public required string Name { get; set; }
    public required FamilyMemberRole Role { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public List<string>? Tags { get; set; }
}
