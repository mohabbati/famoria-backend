using System.Text.Json.Serialization;

namespace Famoria.Domain.Entities;

public abstract class FamilyItemPayload
{
    [JsonIgnore]
    public abstract FamilyItemSource Source { get; }

    public string Type => Source.ToString().ToLowerInvariant();
}
