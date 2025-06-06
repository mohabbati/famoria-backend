using Famoria.Application.Interfaces;
using Famoria.Application.Services.Integrations;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Famoria.Application.Services;

public class GoogleOAuthHelper
{
    private readonly IOptionsMonitor<GoogleAuthSettings> _settings;
    private readonly HttpClient _httpClient;
    private readonly IMailOAuthProvider _mailOAuthProvider;
    private readonly IGoogleJwtValidator _jwtValidator;

    public GoogleOAuthHelper(
        HttpClient httpClient, 
        IOptionsMonitor<GoogleAuthSettings> settings,
        IMailOAuthProvider mailOAuthProvider,
        IGoogleJwtValidator jwtValidator)
    {
        _httpClient = httpClient;
        _settings = settings;
        _mailOAuthProvider = mailOAuthProvider;
        _jwtValidator = jwtValidator;
    }

    public string BuildAuthUrl(string state)
    {
        var cfg = _settings.CurrentValue;
        var scopes = string.Join(" ", cfg.Scopes);
        return $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={Uri.EscapeDataString(cfg.ClientId)}&redirect_uri={Uri.EscapeDataString(cfg.RedirectUri)}&scope={Uri.EscapeDataString(scopes)}&state={Uri.EscapeDataString(state)}&prompt=select_account";
    }

    public async Task<GoogleJsonWebSignature.Payload> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        // Use the IMailOAuthProvider implementation to exchange the code
        var tokenResult = await _mailOAuthProvider.ExchangeCodeAsync(code, ct);
        
        // Use the injected validator to validate the ID token
        var payload = await _jwtValidator.ValidateAsync(tokenResult.IdToken);
        return payload;
    }
}
