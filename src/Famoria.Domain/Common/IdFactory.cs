namespace Famoria.Domain.Common;

/// <summary>
/// Utility methods for generating identifiers suitable for Cosmos DB, URLs, filenames, and other keys.
/// </summary>
public static class IdFactory
{
    /// <summary>
    /// Generates a 32-character lowercase GUID string (no dashes).
    /// Ideal for Cosmos DB keys, URLs, and filenames.
    /// </summary>
    public static string NewGuidId() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Generates a deterministic identifier based on the specified input.
    /// Uses the first 12 bytes (24 hex characters) of the SHA-256 hash.
    /// Suitable for scenarios where you need consistent IDs from the same input.
    /// </summary>
    /// <param name="input">The source string used to generate a deterministic ID.</param>
    /// <returns>A 24-character lowercase hexadecimal string.</returns>
    public static string CreateDeterministicId(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input must be a non-empty string.", nameof(input));

        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);

        // Take first 12 bytes (24 hex chars) for 96-bit identifier
        return Convert.ToHexStringLower(hash, 0, 12);
    }
}
