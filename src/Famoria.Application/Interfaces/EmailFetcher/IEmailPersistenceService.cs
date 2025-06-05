namespace Famoria.Application.Interfaces;

public interface IEmailPersistenceService
{
    /// <summary>
    /// Persists an email and its attachments to blob storage and Cosmos DB.
    /// </summary>
    /// <param name="emlContent">Raw email body as .eml string.</param>
    /// <param name="familyId">Target Cosmos partition and blob folder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated ItemId.</returns>
    Task<string> PersistAsync(string emlContent, string familyId, CancellationToken cancellationToken);
}
