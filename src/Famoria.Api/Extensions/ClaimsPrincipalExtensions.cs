using System.Security.Claims;

namespace Famoria.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetClaim(this ClaimsPrincipal user, string type) =>
        user.Claims.FirstOrDefault(c =>
            string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase))?.Value;
}
