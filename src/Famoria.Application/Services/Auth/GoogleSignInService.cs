using System.Security.Claims;
using CosmosKit;
using Famoria.Domain.Entities;
using Microsoft.Azure.Cosmos;

namespace Famoria.Application.Services.Auth;

public class GoogleSignInService
{
    private readonly IRepository<FamoriaUser> _users;
    private readonly JwtService _jwt;

    public GoogleSignInService(IRepository<FamoriaUser> users, JwtService jwt)
    {
        _users = users;
        _jwt = jwt;
    }

    public async Task<string> SignInAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
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

        if (user is null)
        {
            user = new FamoriaUser(sub, email, "Google", sub, []);
            await _users.AddAsync(user, cancellationToken);
        }

        var token = _jwt.Sign(user.Id, user.Email, null);
        return token;
    }
}
