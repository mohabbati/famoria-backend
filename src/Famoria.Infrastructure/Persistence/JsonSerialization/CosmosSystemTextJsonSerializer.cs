using Microsoft.Azure.Cosmos;
using System.Buffers;
using System.Text.Json;

namespace Famoria.Infrastructure.Persistence.JsonSerialization;

public class CosmosSystemTextJsonSerializer : CosmosSerializer
{
    private readonly JsonSerializerOptions _options;

    public CosmosSystemTextJsonSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    public override T FromStream<T>(Stream stream)
    {
        // Ensure the stream is properly disposed after deserialization
        using (stream)
        {
            var result = JsonSerializer.Deserialize<T>(stream, _options);
            if (result is null)
                throw new JsonException($"Deserialization returned null for type {typeof(T)}");
            return result;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        // Use ArrayBufferWriter and Utf8JsonWriter for efficient serialization
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            JsonSerializer.Serialize(writer, input, _options);
            writer.Flush();
        }
        return new MemoryStream(buffer.WrittenMemory.ToArray());
    }
}
