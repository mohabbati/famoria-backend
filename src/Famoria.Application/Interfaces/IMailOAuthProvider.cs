namespace Famoria.Application.Integrations.Google;

public interface IMailOAuthProvider
{
    string BuildConsentUrl(string state, string userEmail);
    Task<TokenResult> ExchangeCodeAsync(string code, CancellationToken ct);
}

public record TokenResult(
    string AccessToken,
    string? RefreshToken,
    int ExpiresInSeconds,
    string UserEmail);
