namespace Famoria.Api.Identity;

/// <summary>
/// Service for signing and validating JSON Web Tokens (JWTs).
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a signed token for the specified user with optional roles.
    /// </summary>
    /// <remarks>The generated token can be used for authentication and authorization purposes.  Ensure that
    /// the user information and roles provided are accurate and valid for the intended use case.</remarks>
    /// <param name="userDto">The user information used to generate the token.</param>
    /// <param name="roles">An optional collection of roles to include in the token. If null, no roles will be included.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the signed token as a string.</returns>
    Task<string> SignAsync(FamoriaUserDto userDto, IEnumerable<string>? roles = null);
}
