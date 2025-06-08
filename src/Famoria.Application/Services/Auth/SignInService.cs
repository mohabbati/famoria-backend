using System.Security.Claims;
using Microsoft.Azure.Cosmos;

namespace Famoria.Application.Services;

public class SignInService : ISignInService
{
    private readonly IRepository<FamoriaUser> _users;
    private readonly IJwtService _jwt;

    public SignInService(IRepository<FamoriaUser> users, IJwtService jwt)
    {
        _users = users;
        _jwt = jwt;
    }

    public async Task<string> SignInAsync(string provider, ClaimsPrincipal principal, CancellationToken cancellationToken = default)
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
            user = new FamoriaUser(sub, email, provider, sub, []);
            await _users.AddAsync(user, cancellationToken);
        }

        var token = _jwt.Sign(user.Id, user.Email, null);
        return token;
    }
}
