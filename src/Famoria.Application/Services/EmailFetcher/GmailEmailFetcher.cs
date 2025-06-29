using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using GoogleHttpClientFactory = Google.Apis.Http.IHttpClientFactory;
using Google.Apis.Services;
using Google;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Famoria.Application.Models;

namespace Famoria.Application.Services;

public class GmailEmailFetcher : IEmailFetcher
{
    private readonly ILogger<GmailEmailFetcher> _logger;
    private readonly IRepository<UserLinkedAccount> _repository;
    private readonly IAesCryptoService _cryptoService;
    private readonly HttpClient _httpClient;
    private readonly GoogleHttpClientFactory? _gmailHttpClientFactory;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public GmailEmailFetcher(
        ILogger<GmailEmailFetcher> logger,
        IRepository<UserLinkedAccount> repository,
        IAesCryptoService cryptoService,
        HttpClient httpClient,
        IConfiguration configuration,
        GoogleHttpClientFactory? gmailHttpClientFactory = null)
    {
        _logger = logger;
        _repository = repository;
        _cryptoService = cryptoService;
        _httpClient = httpClient;
        _gmailHttpClientFactory = gmailHttpClientFactory;
        _clientId = configuration["Auth:Google:ClientId"] ?? throw new InvalidOperationException("Google client id missing");
        _clientSecret = configuration["Auth:Google:ClientSecret"] ?? throw new InvalidOperationException("Google client secret missing");
    }

    private record TokenRefreshResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private async Task<string> RefreshAccessTokenAsync(string userEmail, CancellationToken cancellationToken)
    {
        var accounts = await _repository.GetAsync(
            x => true/*x.Provider == IntegrationProvider.Google && x.LinkedAccount == userEmail && x.IsActive*/,
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
            ["grant_type"] = "refresh_token",
            ["scope"] = "https://mail.google.com/"
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

    public async Task<IList<RawEmail>> GetNewEmailsAsync(string userEmail, string accessToken, DateTime since, CancellationToken cancellationToken)
    {
        var emails = new List<RawEmail>();
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Famoria.EmailFetcher",
            };
            if (_gmailHttpClientFactory is not null)
            {
                initializer.HttpClientFactory = _gmailHttpClientFactory;
            }

            using var service = new GmailService(initializer);

            var sinceSeconds = new DateTimeOffset(since).ToUnixTimeSeconds();
            var listRequest = service.Users.Messages.List("me");
            listRequest.LabelIds = new[] { "INBOX" };
            // Exclude promotional, social, updates and forum categories
            listRequest.Q = $"after:{sinceSeconds} -category:promotions -category:social -category:updates -category:forums";

            ListMessagesResponse list;
            try
            {
                list = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                accessToken = await RefreshAccessTokenAsync(userEmail, cancellationToken).ConfigureAwait(false);
                credential = GoogleCredential.FromAccessToken(accessToken);
                initializer.HttpClientInitializer = credential;
                using var retryService = new GmailService(initializer);
                list = await retryService.Users.Messages.List("me").ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            if (list.Messages == null) return emails;

            foreach (var msg in list.Messages)
            {
                try
                {
                    var getRequest = service.Users.Messages.Get("me", msg.Id);
                    getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                    var rawResponse = await getRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(rawResponse.Raw))
                    {
                        var padded = rawResponse.Raw.Replace('-', '+').Replace('_', '/');
                        switch (padded.Length % 4)
                        {
                            case 2: padded += "=="; break;
                            case 3: padded += "="; break;
                        }
                        var bytes = Convert.FromBase64String(padded);
                        emails.Add(new(Encoding.UTF8.GetString(bytes), rawResponse.Id, rawResponse.ThreadId, rawResponse.HistoryId.ToString(), rawResponse.LabelIds));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch or process message {MessageId}: {Message} (CorrelationId: {CorrelationId})",
                        msg.Id, ex.Message, correlationId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching emails for {LinkedAccount}: {Message} (CorrelationId: {CorrelationId})",
                userEmail, ex.Message, correlationId);
            throw;
        }

        return emails;
    }
}
