using Famoria.Application.Services.Integrations;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Famoria.Application.Services;

public class GoogleOAuthHelper
{
    private readonly IOptionsMonitor<GoogleAuthSettings> _settings;
    private readonly HttpClient _httpClient;

    public GoogleOAuthHelper(HttpClient httpClient, IOptionsMonitor<GoogleAuthSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public string BuildAuthUrl(string state)
    {
        var cfg = _settings.CurrentValue;
        var scopes = string.Join(" ", cfg.Scopes);
        return $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={Uri.EscapeDataString(cfg.ClientId)}&redirect_uri={Uri.EscapeDataString(cfg.RedirectUri)}&scope={Uri.EscapeDataString(scopes)}&state={Uri.EscapeDataString(state)}&prompt=select_account";
    }

    public async Task<GoogleJsonWebSignature.Payload> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        var cfg = _settings.CurrentValue;
        var token = await GoogleAuth.ExchangeCodeForTokensAsync(cfg.ClientId, cfg.ClientSecret, code, cfg.RedirectUri, ct, _httpClient);
        // Validate id_token and return payload
        var payload = await GoogleJsonWebSignature.ValidateAsync(token.IdToken);
        return payload;
    }
}
