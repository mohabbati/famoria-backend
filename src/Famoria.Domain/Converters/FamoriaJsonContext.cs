using System.Text.Json.Serialization;

namespace Famoria.Domain.Converters;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(FamilyItem))]
[JsonSerializable(typeof(EmailPayload))]
[JsonSerializable(typeof(CalendarPayload))]
public partial class FamoriaJsonContext : JsonSerializerContext { }


// Add it to the API project's Program.cs
//services.Configure<JsonOptions>(options =>
//{
//    options.JsonSerializerOptions.Converters.Add(new FamilyItemPayloadConverter());
//});


//var client = new CosmosClient(connectionString, new CosmosClientOptions
//{
//    Serializer = new CosmosSystemTextJsonSerializer(new JsonSerializerOptions
//    {
//        TypeInfoResolver = FamoriaJsonContext.Default,
//        Converters = { new FamilyItemPayloadConverter() }
//    })
//});


//using System.Text.Json;
//using Microsoft.Azure.Cosmos;

//public class CosmosSystemTextJsonSerializer : CosmosSerializer
//{
//    private readonly JsonSerializerOptions _options;

//    public CosmosSystemTextJsonSerializer(JsonSerializerOptions options)
//    {
//        _options = options;
//    }

//    public override T FromStream<T>(Stream stream)
//    {
//        return JsonSerializer.Deserialize<T>(stream, _options)!;
//    }

//    public override Stream ToStream<T>(T input)
//    {
//        var stream = new MemoryStream();
//        JsonSerializer.Serialize(stream, input, _options);
//        stream.Position = 0;
//        return stream;
//    }
//}
