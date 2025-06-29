using System.Text.Json;
using System.Text.Json.Serialization;

namespace Famoria.Domain.Converters;

public class FamilyItemPayloadConverter : JsonConverter<FamilyItemPayload>
{
    public override FamilyItemPayload? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        if (!jsonDoc.RootElement.TryGetProperty("source", out JsonElement typeProp))
            throw new JsonException("Missing Type discriminator.");

        var json = jsonDoc.RootElement.GetRawText();

        return typeProp.GetString()?.ToLowerInvariant() switch
        {
            "email" => JsonSerializer.Deserialize(json, FamoriaJsonContext.Default.EmailPayload),
            "calendar" => JsonSerializer.Deserialize(json, FamoriaJsonContext.Default.CalendarPayload),
            _ => throw new JsonException($"Unknown payload type: {typeProp.GetString()}")
        };
    }

    public override void Write(Utf8JsonWriter writer, FamilyItemPayload value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}
