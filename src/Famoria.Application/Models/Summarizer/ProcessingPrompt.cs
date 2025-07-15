namespace Famoria.Application.Models;

/// <summary>
/// Payload sent to the LLM for summarization and task extraction.
/// </summary>
/// <summary>
/// Generic prompt payload sent to the LLM for processing different types of family items.
/// </summary>
public record ProcessingPrompt(
    /// <summary>Unique identifier of the family item.</summary>
    string ItemId,
    /// <summary>Type of the source item (e.g., "Email", "CalendarEvent").</summary>
    string SourceType,
    /// <summary>Timestamp when the item was created or received.</summary>
    DateTimeOffset ReceivedAt,
    /// <summary>Language code for content processing (e.g., "en","de").</summary>
    string Language,
    /// <summary>Main textual content extracted from the item.</summary>
    string ContentText,
    /// <summary>Dictionary mapping child names to their associated tags.</summary>
    Dictionary<string, List<string>> MemberTagsByName,
    /// <summary>Additional metadata for extensibility (key-value pairs).</summary>
    Dictionary<string, object> Metadata
);
