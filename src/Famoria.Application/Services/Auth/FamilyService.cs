namespace Famoria.Application.Services;

public class FamilyService : IFamilyService
{
    private readonly IRepository<Family> _families;

    public FamilyService(IRepository<Family> families)
    {
        _families = families;
    }

    public async Task<string> CreateAsync(string famoriaUserId, FamilyDto familyDto, CancellationToken cancellationToken = default)
    {
        var family = new Family
        {
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

        var result = await _families.AddAsync(family, cancellationToken);

        return result.Id;
    }
}
