using Famoria.Application.Models; // Assuming FetchedEmailData might be relevant or for consistency
using System.Collections.Generic; // For List<string>
using System.Threading; // For CancellationToken
using System.Threading.Tasks; // For Task

namespace Famoria.Application.Interfaces;

public interface IEmailPersistenceService
{
    /// <summary>
    /// Persists an email and its attachments to blob storage and Cosmos DB.
    /// </summary>
    /// <param name="emlContent">Raw email body as .eml string.</param>
    /// <param name="familyId">Target Cosmos partition and blob folder.</param>
    /// <param name="providerMessageId">Optional provider-specific message ID (e.g., Gmail ID).</param>
    /// <param name="providerConversationId">Optional provider-specific conversation ID (e.g., Gmail Thread ID).</param>
    /// <param name="providerSyncToken">Optional provider-specific sync token (e.g., Gmail History ID).</param>
    /// <param name="labels">Optional list of labels associated with the email (e.g., Gmail labels).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated ItemId.</returns>
    Task<string> PersistAsync(
        string emlContent,
        string familyId,
        string? providerMessageId,
        string? providerConversationId,
        string? providerSyncToken,
        List<string>? labels,
        CancellationToken cancellationToken);
}
