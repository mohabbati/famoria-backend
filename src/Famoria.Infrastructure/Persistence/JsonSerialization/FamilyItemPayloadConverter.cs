using Famoria.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Famoria.Infrastructure.Persistence.JsonSerialization;

public class FamilyItemPayloadConverter : JsonConverter<FamilyItemPayload>
{
    public override FamilyItemPayload? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        if (!jsonDoc.RootElement.TryGetProperty("source", out JsonElement typeProp))
            throw new JsonException("Missing Type discriminator.");

        var typeString = typeProp.GetString()?.ToLowerInvariant();

        return typeString switch
        {
            "email" => JsonSerializer.Deserialize(jsonDoc.RootElement, typeof(EmailPayload), FamoriaJsonContext.Default) as FamilyItemPayload,
            "calendar" => JsonSerializer.Deserialize(jsonDoc.RootElement, typeof(CalendarPayload), FamoriaJsonContext.Default) as FamilyItemPayload,
            _ => throw new JsonException($"Unknown payload type: {typeString}")
        };
    }

    public override void Write(Utf8JsonWriter writer, FamilyItemPayload value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}
