namespace Famoria.Domain.Enums;

/// <summary>
/// Represents the possible reasons for an AI-related error.
/// </summary>
public enum AiErrorReason
{
    /// <summary>
    /// Failed to parse the .eml or one of its attachments (e.g. corrupt PDF).
    /// </summary>
    AttachmentParseFail,

    /// <summary>
    /// The LLM call timed out (exceeded 30 s).
    /// </summary>
    PromptTimeout,

    /// <summary>
    /// The LLM returned invalid or unparseable JSON.
    /// </summary>
    AIInvalidJson,

    /// <summary>
    /// The total attachment size exceeded the 20 MB guardrail.
    /// </summary>
    OversizeBlob,

    /// <summary>
    /// Any other unexpected failure.
    /// </summary>
    Unknown
}
