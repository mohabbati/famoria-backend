using Famoria.Application.Models.Dtos;

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

    public async Task<string> SignInAsync(FamoriaUserDto userDto, CancellationToken cancellationToken = default)
    {
        var id = $"{userDto.Provider}-{userDto.ProviderSub}";

        var user = await _users.GetByAsync(new FamoriaUser(id), cancellationToken);
        
        if (user is null)
        {
            user = new FamoriaUser(id, userDto.Email, userDto.Provider, userDto.ProviderSub, [])
                { GivenName = userDto.GivenName, FirstName = userDto.LastName, LastName = userDto.LastName };

            await _users.UpsertAsync(user, cancellationToken);
        }

        var token = _jwt.Sign(user.Id, user.Email, null);

        return token;
    }
}
