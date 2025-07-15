using Famoria.Domain.Entities;
using System.Text.Json.Serialization;

namespace Famoria.Infrastructure.Persistence.JsonSerialization;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(FamoriaUser))]
[JsonSerializable(typeof(FamoriaUser[]))]
[JsonSerializable(typeof(Family))]
[JsonSerializable(typeof(FamilyItem))]
[JsonSerializable(typeof(FamilyItem[]))]
[JsonSerializable(typeof(FamilyItemAudit))]
[JsonSerializable(typeof(EmailPayload))]
[JsonSerializable(typeof(CalendarPayload))]
[JsonSerializable(typeof(UserLinkedAccount))]
[JsonSerializable(typeof(UserLinkedAccount[]))]
public partial class FamoriaJsonContext : JsonSerializerContext { }
