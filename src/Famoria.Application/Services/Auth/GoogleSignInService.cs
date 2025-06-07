using System.Security.Claims;
using CosmosKit;
using Famoria.Domain.Common;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums;
using Microsoft.Azure.Cosmos;

namespace Famoria.Application.Services.Auth;

public class GoogleSignInService
{
    private readonly IRepository<FamoriaUser> _users;
    private readonly IRepository<Family> _families;
    private readonly JwtService _jwt;

    public GoogleSignInService(IRepository<FamoriaUser> users, IRepository<Family> families, JwtService jwt)
    {
        _users = users;
        _families = families;
        _jwt = jwt;
    }

    public async Task<(string Token, string FamilyId)> SignInAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var sub = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? 
                 principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? 
                 throw new InvalidOperationException("sub missing");
        var email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? 
                   throw new InvalidOperationException("email missing");
        var name = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? email;

        FamoriaUser? user = null;
        try
        {
            user = await _users.GetByAsync(new FamoriaUser(sub), cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }

        string familyId;
        if (user is null)
        {
            familyId = IdGenerator.NewId();
            var family = new Family
            {
                Id = familyId,
                DisplayName = name,
                Members =
                [
                    new FamilyMember
                    {
                        UserId = sub,
                        Name = name,
                        Role = FamilyMemberRole.Parent
                    }
                ],
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            await _families.AddAsync(family, cancellationToken);

            user = new FamoriaUser(sub, email, "Google", sub, [familyId]);
            await _users.AddAsync(user, cancellationToken);
        }
        else
        {
            familyId = user.FamilyIds.First();
        }

        var token = _jwt.Sign(user.Id, user.Email, familyId);
        return (token, familyId);
    }
}
