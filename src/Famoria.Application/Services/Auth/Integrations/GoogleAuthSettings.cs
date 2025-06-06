namespace Famoria.Application.Services.Integrations;

public class GoogleAuthSettings
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string ProjectId { get; init; }
    public required string AuthUri { get; init; }
    public required string TokenUri { get; init; }
    public required string AuthProviderX509CertUrl { get; init; }
    public required string RedirectUri { get; init; }
    public string[] Scopes { get; init; } = { "https://mail.google.com/" };
}
