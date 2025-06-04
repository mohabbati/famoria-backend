namespace Famoria.Application.Integrations.Google;

public class GoogleAuthSettings
{
    public required string ClientId     { get; init; }
    public required string ClientSecret { get; init; }
    public required string RedirectUri  { get; init; }
    public string[] Scopes { get; init; } = { "https://mail.google.com/" };
}
