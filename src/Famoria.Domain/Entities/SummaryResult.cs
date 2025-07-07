namespace Famoria.Domain.Entities;

/// <summary>
/// Combined result structure stored on the FamilyItem for application use.
/// </summary>
public record SummaryResult(
    /// <summary>Concise summary of the email content.</summary>
    string Summary,
    /// <summary>List of actionable items extracted from the email.</summary>
    List<string> ActionItems,
    /// <summary>Fixed set of labels for categorization.</summary>
    List<string> Labels,
    /// <summary>Free-form keywords or tags for search and filtering.</summary>
    List<string> Keywords,
    /// <summary>Priority level of the email.</summary>
    string Priority,
    /// <summary>Matched recipients or "Undetected".</summary>
    List<string> Recipients,
    /// <summary>Detection status (Matched, Undetected, Broadcast).</summary>
    string DetectionStatus
);
