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

        // Create options that are more lenient with required properties
        var deserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // This allows missing required properties during deserialization
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        return typeProp.GetString()?.ToLowerInvariant() switch
        {
            "email" => JsonSerializer.Deserialize<EmailPayload>(json, deserializeOptions),
            "calendar" => JsonSerializer.Deserialize<CalendarPayload>(json, deserializeOptions),
            _ => throw new JsonException($"Unknown payload type: {typeProp.GetString()}")
        };
    }

    public override void Write(Utf8JsonWriter writer, FamilyItemPayload value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}
