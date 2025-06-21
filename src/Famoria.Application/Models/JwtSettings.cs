namespace Famoria.Application.Models;

public class JwtSettings
{
    /// <summary>Secret used to sign tokens (minimum 16 characters).</summary>
    public required string Secret { get; init; }
    /// <summary>Token issuer (iss claim).</summary>
    public required string Issuer { get; init; }
    /// <summary>Token audience (aud claim).</summary>
    public required string Audience { get; init; }
    /// <summary>Lifetime of the token.</summary>
    public TimeSpan TokenLifetime { get; init; }
}
