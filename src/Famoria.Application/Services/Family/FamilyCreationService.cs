using System.Security.Claims;
using CosmosKit;
using Famoria.Domain.Common;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums;
using Microsoft.Azure.Cosmos;

namespace Famoria.Application.Services.Family;

public class FamilyCreationService
{
    private readonly IRepository<Family> _families;
    private readonly IRepository<FamoriaUser> _users;

    public FamilyCreationService(IRepository<Family> families, IRepository<FamoriaUser> users)
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

        FamoriaUser? user = null;
        try
        {
            user = await _users.GetByAsync(new FamoriaUser(sub), cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }

        if (user is null)
        {
            user = new FamoriaUser(sub, email, "Google", sub, [familyId]);
            await _users.AddAsync(user, cancellationToken);
        }
        else
        {
            var ids = user.FamilyIds.ToList();
            if (!ids.Contains(familyId))
                ids.Add(familyId);
            var updated = new FamoriaUser(user.Id, user.Email, user.Provider, user.ExternalSub, ids);
            await _users.AddAsync(updated, cancellationToken);
        }

        return familyId;
    }
}
