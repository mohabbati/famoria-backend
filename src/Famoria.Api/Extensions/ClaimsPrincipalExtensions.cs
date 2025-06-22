using System.Security.Claims;

namespace Famoria.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetClaim(this ClaimsPrincipal user, string type) =>
        user.Claims.FirstOrDefault(c =>
            string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase))?.Value;

    public static string? FamoriaUserId(this ClaimsPrincipal user) =>
        user.GetClaim(ClaimTypes.NameIdentifier);

    public static string? Email(this ClaimsPrincipal user) =>
        user.GetClaim(ClaimTypes.Email);

    public static string? FirstName(this ClaimsPrincipal user) =>
        user.GetClaim(ClaimTypes.GivenName);

    public static string? LastName(this ClaimsPrincipal user) =>
        user.GetClaim(ClaimTypes.Surname);

    public static string? Name(this ClaimsPrincipal user) =>
        user.GetClaim(ClaimTypes.Name);

    public static string? FamilyId(this ClaimsPrincipal user) =>
        user.GetClaim("family_id");
}
