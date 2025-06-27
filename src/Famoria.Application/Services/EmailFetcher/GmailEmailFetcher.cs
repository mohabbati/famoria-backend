using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Famoria.Application.Services;

public class GmailEmailFetcher : IEmailFetcher
{
    private readonly ILogger<GmailEmailFetcher> _logger;
    private readonly IImapClientWrapper _imapClient;
    private readonly IRepository<UserLinkedAccount> _repository;
    private readonly IAesCryptoService _cryptoService;
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public GmailEmailFetcher(
        ILogger<GmailEmailFetcher> logger,
        IImapClientWrapper imapClient,
        IRepository<UserLinkedAccount> repository,
        IAesCryptoService cryptoService,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _imapClient = imapClient;
        _repository = repository;
        _cryptoService = cryptoService;
        _httpClient = httpClient;
        _clientId = configuration["Auth:Google:ClientId"] ?? throw new InvalidOperationException("Google client id missing");
        _clientSecret = configuration["Auth:Google:ClientSecret"] ?? throw new InvalidOperationException("Google client secret missing");
    }

    private record TokenRefreshResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private async Task<string> RefreshAccessTokenAsync(string userEmail, CancellationToken cancellationToken)
    {
        var accounts = await _repository.GetAsync(
            x => x.Provider == IntegrationProvider.Google && x.LinkedAccount == userEmail && x.IsActive,
            cancellationToken: cancellationToken);
        var account = accounts.FirstOrDefault() ?? throw new InvalidOperationException($"No linked Gmail account for {userEmail}");
        if (string.IsNullOrEmpty(account.RefreshToken))
            throw new InvalidOperationException($"No refresh token stored for {userEmail}");

        var refreshToken = _cryptoService.Decrypt(account.RefreshToken);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        });

        using var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var tokenResponse = await JsonSerializer.DeserializeAsync<TokenRefreshResponse>(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to parse token response");

        account.AccessToken = _cryptoService.Encrypt(tokenResponse.AccessToken);
        account.TokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        await _repository.UpsertAsync(account, cancellationToken).ConfigureAwait(false);

        return tokenResponse.AccessToken;
    }

    public async Task<List<string>> GetNewEmailsAsync(string userEmail, string accessToken, DateTime since, CancellationToken cancellationToken)
    {
        var ct = cancellationToken;
        var emlList = new List<string>();
        var correlationId = Guid.NewGuid().ToString();
        var connected = false;
        try
        {
            await _imapClient.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect, ct).ConfigureAwait(false);
            connected = true;
            var sasl = new SaslMechanismOAuth2(userEmail, accessToken);
            try
            {
                await _imapClient.AuthenticateAsync(sasl, ct).ConfigureAwait(false);
            }
            catch (AuthenticationException)
            {
                accessToken = await RefreshAccessTokenAsync(userEmail, ct).ConfigureAwait(false);
                sasl = new SaslMechanismOAuth2(userEmail, accessToken);
                await _imapClient.AuthenticateAsync(sasl, ct).ConfigureAwait(false);
            }

            var inbox = await _imapClient.GetInboxAsync(ct).ConfigureAwait(false);

            // Only fetch emails received after 'since'
            var query = SearchQuery.NotSeen.Or(SearchQuery.Recent).Or(SearchQuery.DeliveredAfter(since));
            var uids = await _imapClient.SearchAsync(inbox, query, ct).ConfigureAwait(false);

            foreach (var uid in uids)
            {
                try
                {
                    var message = await _imapClient.GetMessageAsync(inbox, uid, ct).ConfigureAwait(false);
                    using var stream = new MemoryStream();
                    await message.WriteToAsync(stream, ct).ConfigureAwait(false);
                    var emlContent = Encoding.UTF8.GetString(stream.ToArray());
                    emlList.Add(emlContent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch or process message UID {Uid}: {Message} (CorrelationId: {CorrelationId})",
                        uid, ex.Message, correlationId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching emails for {LinkedAccount}: {Message} (CorrelationId: {CorrelationId})",
                userEmail, ex.Message, correlationId);
            throw;
        }
        finally
        {
            if (connected)
            {
                try
                {
                    await _imapClient.DisconnectAsync(true, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to disconnect IMAP client (CorrelationId: {CorrelationId})", correlationId);
                }
            }

            _imapClient.Dispose();
        }

        return emlList;
    }
}
