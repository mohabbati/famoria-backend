namespace Famoria.Domain.Entities;

public class FamilyItem : AuditableEntity
{
    public required string FamilyId { get; init; }
    public required FamilyItemSource Source { get; set; }
    public required FamilyItemPayload Payload { get; set; }
    public FamilyItemStatus Status { get; set; } = FamilyItemStatus.New;
    public SummaryResult? Summary { get; set; }

    /// <summary>Reason for any AI processing error.</summary>
    public AiErrorReason? AiErrorReason { get; set; }
    /// <summary>Number of retries attempted for AI processing.</summary>
    public int AiRetryCount { get; set; }
}
