namespace Famoria.Domain.Common;

public static class IdGenerator
{
    /// <summary>
    /// Generates a 32-character lowercase GUID string (no dashes).
    /// Ideal for Cosmos DB keys, URLs, and filenames.
    /// </summary>
    public static string NewId() => Guid.NewGuid().ToString("N");
}
