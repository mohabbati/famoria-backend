using Famoria.Domain.Enums;

namespace Famoria.Application.Models.Dtos;

public record FamilyMemberDto
(
    string UserId,
    string Name,
    FamilyMemberRole Role = FamilyMemberRole.Child,
    string? Color = default,
    string? Icon = default,
    List<string>? Tags = default
);
