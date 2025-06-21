using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Famoria.Application.Services;

public class JwtService : IJwtService
{
    private readonly byte[] _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _tokenLifetime;

    public JwtService(IOptionsMonitor<JwtSettings> options)
    {
        var settings = options.CurrentValue;
        if (string.IsNullOrWhiteSpace(settings.Secret) || settings.Secret.Length < 16)
            throw new ArgumentException("JWT Secret must be at least 16 characters.");

        _secret = Encoding.UTF8.GetBytes(settings.Secret);
        _issuer = settings.Issuer;
        _audience = settings.Audience;
        _tokenLifetime = settings.TokenLifetime;
    }

    public string Sign(
        string subject,
        string email,
        string? familyId = null,
        IEnumerable<string>? roles = null)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(ClaimTypes.NameIdentifier, subject),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(familyId))
            claims.Add(new("family_id", familyId));

        if (roles != null)
        {
            foreach (var role in roles)
                claims.Add(new(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(_secret);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(_tokenLifetime),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

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
