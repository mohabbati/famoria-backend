using System.Text.Json.Serialization;

namespace Famoria.Domain.Common;

public abstract class EntityBase<TKey>
{
    [JsonPropertyName("id")]
    public TKey Id { get; set; } = default!;
}

public abstract class EntityBase : EntityBase<string>;
