using Famoria.Application.Models;
using MimeKit;

namespace Famoria.Application.Interfaces;

public interface IEmailService
{
    Task<string> DownloadBlobAsync(string blobPath, CancellationToken cancellationToken);

    /// <summary>
    /// Parses the raw email content into a <see cref="MimeMessage"/> object asynchronously.
    /// </summary>
    /// <param name="rawEmailContent">The raw email content as a string. Must be a valid MIME format.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the parsed <see cref="MimeMessage"/>
    /// object.</returns>
    Task<MimeMessage> ParseAsync(string rawEmailContent, CancellationToken cancellationToken);

    /// <summary>
    /// Extracts the plain text content from the provided raw email content asynchronously.
    /// </summary>
    /// <param name="rawEmailContent">The raw email content, typically in MIME format, from which text will be extracted. Cannot be null or empty.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the extracted plain text content of
    /// the email.</returns>
    Task<string> ExtractTextAsync(string rawEmailContent, CancellationToken cancellationToken);

    /// <summary>
    /// Persists an email and its attachments to blob storage and Cosmos DB.
    /// </summary>
    /// <param name="emlContent">Raw email body as .eml string.</param>
    /// <param name="familyId">Target Cosmos partition and blob folder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated ItemId.</returns>
    Task<string> PersistAsync(RawEmail rawEmail, string familyId, CancellationToken cancellationToken);
}
