using System.Text.Json.Serialization;

namespace Famoria.Domain.Converters;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(FamoriaUser))]
[JsonSerializable(typeof(FamoriaUser[]))]
[JsonSerializable(typeof(Family))]
[JsonSerializable(typeof(FamilyItem))]
[JsonSerializable(typeof(EmailPayload))]
[JsonSerializable(typeof(CalendarPayload))]
[JsonSerializable(typeof(UserLinkedAccount))]
public partial class FamoriaJsonContext : JsonSerializerContext { }
