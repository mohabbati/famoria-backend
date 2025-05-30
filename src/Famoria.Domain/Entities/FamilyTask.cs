namespace Famoria.Domain.Entities;

public class FamilyTask : EntityBase
{
    public required string FamilyId { get; init; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public FamilyTaskStatus Status { get; set; } = FamilyTaskStatus.Pending;
    public string CreatedFromItemId { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
}
