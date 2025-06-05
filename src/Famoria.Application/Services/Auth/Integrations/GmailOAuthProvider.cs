using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Famoria.Application.Interfaces;

namespace Famoria.Application.Services.Integrations;

public class GmailOAuthProvider : IMailOAuthProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<GoogleAuthSettings> _settings;
    private readonly ILogger<GmailOAuthProvider> _logger;

    public GmailOAuthProvider(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<GoogleAuthSettings> settings,
        ILogger<GmailOAuthProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
    }

    public string BuildConsentUrl(string state, string userEmail)
    {
        var settings = _settings.CurrentValue;
        var scopes = string.Join(" ", settings.Scopes);
        var url = $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={Uri.EscapeDataString(settings.ClientId)}&redirect_uri={Uri.EscapeDataString(settings.RedirectUri)}&scope={Uri.EscapeDataString(scopes)}&access_type=offline&prompt=consent&state={Uri.EscapeDataString(state)}&login_hint={Uri.EscapeDataString(userEmail)}";
        return url;
    }

    public async Task<TokenResult> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        var settings = _settings.CurrentValue;
        var client = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", settings.ClientId),
            new KeyValuePair<string, string>("client_secret", settings.ClientSecret),
            new KeyValuePair<string, string>("redirect_uri", settings.RedirectUri),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });
        var response = await client.PostAsync("https://oauth2.googleapis.com/token", content, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()!;
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = root.GetProperty("expires_in").GetInt32();
        var idToken = root.GetProperty("id_token").GetString()!;
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);
        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "";
        return new TokenResult(accessToken, refreshToken, expiresIn, email);
    }
}
