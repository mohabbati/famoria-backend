namespace Famoria.Application.Models.Summarizer;

/// <summary>
/// Response schema returned by the LLM after processing the prompt.
/// </summary>
public class AiResponse
{
    /// <summary>Concise summary (1–2 sentences) of the email content.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>List of actionable items extracted from the content.</summary>
    public List<string> ActionItems { get; set; } = new();

    /// <summary>Fixed set of labels assigned to the email.</summary>
    public List<string> Labels { get; set; } = new();

    /// <summary>Free-form keywords or tags extracted by the model.</summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>Priority level decided by the model ("High" or "Normal").</summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>List of recipients matched or default ("Undetected").</summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>Detection status: Matched, Undetected, or Broadcast.</summary>
    public string DetectionStatus { get; set; } = string.Empty;
}
