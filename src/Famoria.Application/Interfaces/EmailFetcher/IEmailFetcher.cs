using Famoria.Application.Models;

namespace Famoria.Application.Interfaces;

public interface IEmailFetcher
{
    /// <summary>
    /// Downloads new emails from the user's inbox using OAuth2, including provider-specific metadata.
    /// </summary>
    /// <param name="userEmail">The email address (used for authentication).</param>
    /// <param name="accessToken">OAuth2 token.</param>
    /// <param name="since">Only fetch emails received after this time (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of <see cref="FetchedEmailData"/> containing EML content and metadata.</returns>
    Task<List<FetchedEmailData>> GetNewEmailsAsync(string userEmail, string accessToken, DateTime since, CancellationToken cancellationToken);
}
