using System.Text.Json.Serialization;

using Famoria.Domain.Converters;

namespace Famoria.Domain.Entities;

[JsonConverter(typeof(FamilyItemPayloadConverter))]
public abstract class FamilyItemPayload
{
    [JsonIgnore]
    public abstract FamilyItemSource Source { get; }

    public string Type => Source.ToString().ToLowerInvariant();
}
