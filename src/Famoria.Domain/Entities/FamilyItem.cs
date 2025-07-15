namespace Famoria.Domain.Entities;

public class FamilyItem : AuditableEntity
{
    public required string FamilyId { get; init; }
    public required FamilyItemSource Source { get; set; }
    public required FamilyItemPayload Payload { get; set; }
    public FamilyItemStatus Status { get; set; } = FamilyItemStatus.New;
    public SummaryResult? Summary { get; set; }
    public AiErrorReason AiErrorReason { get; set; }
    public int AiRetryCount { get; set; }
}
