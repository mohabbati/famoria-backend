namespace Famoria.Application.Models.Dtos;

public record FamoriaUserDto(
    string Id,
    string GivenName,
    string FirstName,
    string LastName,
    string Email,
    string Provider,
    string ProviderSub,
    IList<string> FamilyIds);
