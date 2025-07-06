using System.Text.Json.Serialization;

namespace Famoria.Domain.Entities;

public abstract class FamilyItemPayload
{
    [JsonIgnore]
    public abstract FamilyItemSource Source { get; }
    public DateTimeOffset ReceivedAt { get; set; }

    public string Type => Source.ToString().ToLowerInvariant();
}
