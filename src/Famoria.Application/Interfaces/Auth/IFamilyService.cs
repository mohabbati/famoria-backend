namespace Famoria.Application.Interfaces;

public interface IFamilyService
{
    Task<string> CreateAsync(string famoriaUserId, FamilyDto familyDto, CancellationToken cancellationToken = default);
}
