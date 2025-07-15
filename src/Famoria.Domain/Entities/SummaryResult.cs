namespace Famoria.Domain.Entities;

/// <summary>
/// Response schema returned by the LLM after processing the prompt.
/// </summary>
/// <param name="Summary">Concise summary (1–2 sentences) of the email content.</param>
/// <param name="ActionItems">List of actionable items extracted from the content.</param>
/// <param name="Keywords">Free-form keywords or tags extracted by the model.</param>
/// <param name="Priority">Priority level decided by the model ("High" or "Normal").</param>
/// <param name="Label">One of: "Material", "Event", "Reminder", or "Information".</param>
/// <param name="MatchedMembers">List of recipients matched by name or tag (or empty).</param>
/// <param name="DetectionStatus">Detection status: "Matched", "Undetected", or "Broadcast".</param>
public record SummaryResult(
    string Summary,
    List<string> ActionItems,
    List<string> Keywords,
    PriorityLevel Priority,
    string Label,
    List<string> MatchedMembers,
    DetectionStatusType DetectionStatus
);
