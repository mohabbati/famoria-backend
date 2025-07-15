namespace Famoria.Domain.Enums;

/// <summary>
/// Represents the possible reasons for an AI-related error.
/// </summary>
public enum AiErrorReason
{
    /// <summary>
    /// Default and any other unexpected failure.
    /// </summary>
    Unknown,

    /// <summary>
    /// The LLM call timed out (exceeded 30 s).
    /// </summary>
    PromptTimeout,

    /// <summary>
    /// The LLM returned invalid or unparseable JSON.
    /// </summary>
    AiInvalidJson,

    /// <summary>
    /// Represents a permanent failure state.
    /// </summary>
    /// <remarks>This enumeration value is used to indicate that an operation has failed in a way that is not
    /// recoverable. It is typically used in scenarios where retrying the operation is not expected to
    /// succeed.</remarks>
    FailedPermanent
}
