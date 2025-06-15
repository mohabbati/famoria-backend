using System.Security.Claims;

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

    public async Task<string> SignInAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var iss = principal.Claims.Select(c => c.Issuer).FirstOrDefault()?.ToLowerInvariant() ??
                 throw new InvalidOperationException("issuer missing");
        var sub = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ??
                 principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ??
                 throw new InvalidOperationException("subject missing");
        var email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ??
                   throw new InvalidOperationException("email missing");
        var name = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
        var firstName = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? string.Empty;
        var lastName = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? string.Empty;

        var id = $"{iss}-{sub}";

        var user = await _users.GetByAsync(new FamoriaUser(id), cancellationToken);
        
        if (user is null)
        {
            user = new FamoriaUser(id, email, iss, sub, [])
                { GivenName = name, FirstName = firstName, LastName = lastName };
            // Insert or update the user record in case it already exists
            await _users.UpsertAsync(user, cancellationToken);
        }

        var token = _jwt.Sign(user.Id, user.Email, null);

        return token;
    }
}
