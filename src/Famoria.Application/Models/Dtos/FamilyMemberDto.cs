using Famoria.Domain.Enums;

namespace Famoria.Application.Models.Dtos;

public record FamilyMemberDto
(
    string Name,
    FamilyMemberRole Role,
    string? Color,
    string? Icon,
    List<string>? Tags
);
