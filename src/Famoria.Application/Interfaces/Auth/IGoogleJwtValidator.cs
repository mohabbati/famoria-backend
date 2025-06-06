namespace Famoria.Application.Interfaces;

using Google.Apis.Auth;

/// <summary>
/// Interface for validating Google JWT tokens
/// </summary>
public interface IGoogleJwtValidator
{
    /// <summary>
    /// Validates a Google ID token and returns the payload
    /// </summary>
    /// <param name="idToken">The ID token to validate</param>
    /// <returns>The validated payload</returns>
    Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken);
}
