namespace Famoria.Application.Interfaces;

public interface IFamilyService
{
    Task<string> CreateAsync(FamilyDto familyDto, CancellationToken cancellationToken = default);
    Task<string> UpdateAsync(UpdateFamilyDto familyDto, CancellationToken cancellationToken = default);
}
