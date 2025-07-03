namespace Famoria.Application.Models.Dtos;

public record UserLinkedAccountDto(
    IntegrationProvider Provider,
    string FamilyId,
    string LinkedAccount, 
    string AccessToken, 
    string? RefreshToken, 
    DateTime LastFetchedAtUtc);
