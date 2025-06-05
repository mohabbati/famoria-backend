namespace Famoria.Application.Interfaces;

public interface IEmailFetcher
{
    /// <summary>
    /// Downloads new emails as raw .eml strings from the user's inbox using OAuth2.
    /// </summary>
    /// <param name="userEmail">The Gmail address (used for authentication).</param>
    /// <param name="accessToken">OAuth2 token</param>
    /// <param name="since">Only fetch emails received after this time (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of raw .eml strings.</returns>
    Task<List<string>> GetNewEmailsAsync(string userEmail, string accessToken, DateTime since, CancellationToken cancellationToken);
}
