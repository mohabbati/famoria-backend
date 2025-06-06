using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Famoria.Application.Services;

public class JwtService
{
    private readonly byte[] _secret;

    public JwtService(IOptionsMonitor<JwtSettings> options)
    {
        _secret = Encoding.UTF8.GetBytes(options.CurrentValue.Secret);
    }

    public string Sign(string subject, string email, string familyId)
    {
        var handler = new JwtSecurityTokenHandler();
        var creds = new SigningCredentials(new SymmetricSecurityKey(_secret), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, subject),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("family_id", familyId)
            },
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);
        return handler.WriteToken(token);
    }

    public ClaimsPrincipal? Validate(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(_secret),
            ValidateIssuerSigningKey = true
        };
        try
        {
            return handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }
}

public class JwtSettings
{
    public required string Secret { get; init; }
}
