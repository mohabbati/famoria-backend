namespace Famoria.Domain.Entities;

/// <summary>
/// Audit record storing the prompt and raw LLM response for a FamilyItem.
/// These documents are stored in the "family-items-audits" container with a 7-day TTL.
/// </summary>
public class FamilyItemAudit : EntityBase
{
    /// <summary>Partition key: the FamilyId to which the item belongs.</summary>
    public string FamilyId { get; set; } = default!;

    /// <summary>Identifier of the FamilyItem that triggered this audit.</summary>
    public string FamilyItemId { get; set; } = default!;

    /// <summary>UTC timestamp when the prompt was sent.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Serialized JSON of the ProcessingPrompt sent to the LLM.</summary>
    public string Prompt { get; set; } = default!;

    /// <summary>Raw JSON response returned by the LLM.</summary>
    public string Response { get; set; } = default!;

    /// <summary>
    /// Optional TTL override in seconds. If not set, container default TTL (604800s) applies.
    /// </summary>
    public int? Ttl { get; set; }
}
