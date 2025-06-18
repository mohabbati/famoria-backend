using Famoria.Domain.Common;
using Famoria.Domain.Enums;
using System.Security.Claims;

namespace Famoria.Application.Services;

public class FamilyService
{
    private readonly IRepository<Family> _families;
    private readonly IRepository<FamoriaUser> _users;

    public FamilyService(IRepository<Family> families, IRepository<FamoriaUser> users)
    {
        _families = families;
        _users = users;
    }

    public async Task<string> CreateAsync(ClaimsPrincipal principal, string displayName, IEnumerable<(string Name, IEnumerable<string>? Tags)>? children = null, CancellationToken cancellationToken = default)
    {
        var sub = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ??
                  principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ??
                  throw new InvalidOperationException("sub missing");
        var email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ??
                     throw new InvalidOperationException("email missing");
        var name = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? email;

        var familyId = IdGenerator.NewId();
        var members = new List<FamilyMember>
        {
            new FamilyMember { UserId = sub, Name = name, Role = FamilyMemberRole.Parent }
        };
        if (children != null)
        {
            foreach (var child in children)
            {
                members.Add(new FamilyMember
                {
                    Name = child.Name,
                    Role = FamilyMemberRole.Child,
                    Tags = child.Tags?.ToList()
                });
            }
        }

        var family = new Family
        {
            Id = familyId,
            DisplayName = displayName,
            Members = members,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        await _families.AddAsync(family, cancellationToken);

        var user = await _users.GetByAsync(new FamoriaUser(sub), cancellationToken);
        
        var ids = user!.FamilyIds.ToList();
        if (!ids.Contains(familyId))
            ids.Add(familyId);
        var updated = new FamoriaUser(user.Id, user.Email, user.Provider, user.ProviderSub, ids);
        await _users.AddAsync(updated, cancellationToken);
        
        return familyId;
    }
}
