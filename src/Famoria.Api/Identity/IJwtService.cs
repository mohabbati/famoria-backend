namespace Famoria.Api.Identity;

/// <summary>
/// Service for signing and validating JSON Web Tokens (JWTs).
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Create a signed JWT containing standard and custom claims.
    /// </summary>
    /// <param name="subject">Unique user identifier (sub claim).</param>
    /// <param name="email">User's email address (email claim).</param>
    /// <param name="familyId">Optional family context (family_id claim).</param>
    /// <param name="roles">Optional collection of roles (role claims).</param>
    /// <returns>Serialized JWT string.</returns>
    string Sign(
        string subject,
        string email,
        string? familyId = null,
        IEnumerable<string>? roles = null
    );
}
