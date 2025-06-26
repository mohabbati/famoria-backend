namespace Famoria.Application.Models.Dtos;

public record UserLinkedAccountDto(
    string FamilyId,
    string LinkedAccount, 
    string AccessToken, 
    string? RefreshToken, 
    DateTime LastFetchedAtUtc);
