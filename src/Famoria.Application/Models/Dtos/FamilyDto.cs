namespace Famoria.Application.Models.Dtos;

public record FamilyDto(string DisplayName, List<FamilyMemberDto> Members);
public record UpdateFamilyDto(string Id, string DisplayName, List<FamilyMemberDto> Members);
