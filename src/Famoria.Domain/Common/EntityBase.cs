using System.Text.Json.Serialization;

namespace Famoria.Domain.Common;

public abstract class EntityBase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    // Cosmos writes this; you never set it on insert
    [JsonPropertyName("_etag")]
    public string ETag { get; set; } = default!;
}
