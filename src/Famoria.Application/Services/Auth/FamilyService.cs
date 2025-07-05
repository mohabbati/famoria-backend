namespace Famoria.Application.Services;

public class FamilyService : IFamilyService
{
    private readonly ICosmosRepository<Family> _families;

    public FamilyService(ICosmosRepository<Family> families)
    {
        _families = families;
    }

    public async Task<string> CreateAsync(FamilyDto familyDto, CancellationToken cancellationToken = default)
    {
        var family = new Family
        {
            Id = IdFactory.NewGuidId(),
            DisplayName = familyDto.DisplayName,
            Members = familyDto.Members?.Select(c => new FamilyMember
            {
                UserId = c.UserId,
                Name = c.Name,
                Role = c.Role,
                Color = c.Color,
                Icon = c.Icon,
                Tags = c.Tags ?? [],
            }).ToList() ?? [],
        };

        await _families.AddAsync(family, cancellationToken);

        return family.Id;
    }

    public async Task<string> UpdateAsync(UpdateFamilyDto familyDto, CancellationToken cancellationToken = default)
    {
        var family = new Family
        {
            Id = familyDto.Id,
            DisplayName = familyDto.DisplayName,
            Members = familyDto.Members?.Select(c => new FamilyMember
            {
                Name = c.Name,
                Role = c.Role,
                Color = c.Color,
                Icon = c.Icon,
                Tags = c.Tags ?? [],
            }).ToList() ?? [],
        };

        await _families.UpdateAsync(family, cancellationToken);

        return family.Id;
    }
}
