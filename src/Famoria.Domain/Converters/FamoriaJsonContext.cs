using System.Text.Json.Serialization;

namespace Famoria.Domain.Converters;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(FamilyItem))]
[JsonSerializable(typeof(EmailPayload))]
[JsonSerializable(typeof(CalendarPayload))]
public partial class FamoriaJsonContext : JsonSerializerContext { }
