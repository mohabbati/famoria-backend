using Famoria.Application.Interfaces;
using Google.Apis.Auth;

namespace Famoria.Application.Services.Auth;

/// <summary>
/// Default implementation of IGoogleJwtValidator that uses GoogleJsonWebSignature
/// </summary>
public class GoogleJwtValidator : IJwtValidator<GoogleJsonWebSignature.Payload>
{
    /// <summary>
    /// Validates a Google ID token using GoogleJsonWebSignature
    /// </summary>
    /// <param name="idToken">The ID token to validate</param>
    /// <returns>The validated payload</returns>
    public async Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken)
    {
        return await GoogleJsonWebSignature.ValidateAsync(idToken);
    }
}
