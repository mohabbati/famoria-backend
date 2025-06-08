namespace Famoria.Application.Interfaces;

/// <summary>
/// Interface for validating Google JWT tokens
/// </summary>
public interface IJwtValidator<TPayload> where TPayload : class
{
    /// <summary>
    /// Validates a JWT token and returns the payload
    /// </summary>
    /// <param name="token">The ID token to validate</param>
    /// <returns>The validated payload</returns>
    Task<TPayload> ValidateAsync(string token);
}
