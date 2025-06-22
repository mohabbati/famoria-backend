namespace Famoria.Application.Services;

public class UserService : IUserService
{
    private readonly IRepository<FamoriaUser> _users;

    public UserService(IRepository<FamoriaUser> users)
    {
        _users = users;
    }

    public async Task<FamoriaUserDto> CreateAsync(FamoriaUserDto userDto, CancellationToken cancellationToken = default)
    {
        var user = new FamoriaUser(userDto.Id, userDto.Email, userDto.Provider, userDto.ProviderSub, [])
                { GivenName = userDto.GivenName, FirstName = userDto.LastName, LastName = userDto.LastName };

        await _users.AddAsync(user, cancellationToken);

        return new FamoriaUserDto(
            user.Id,
            user.GivenName,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Provider,
            user.ProviderSub,
            user.FamilyIds);
    }

    public async Task<FamoriaUserDto?> GetByAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByAsync(new FamoriaUser(id), cancellationToken);

        if (user is null)
            return null;

        return new FamoriaUserDto(
            user.Id,
            user.GivenName,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Provider,
            user.ProviderSub,
            user.FamilyIds);
    }
    public async Task AddFamilyToUserAsync(string userId, string familyId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByAsync(new FamoriaUser(userId), cancellationToken);

        if (user is null)
            throw new InvalidOperationException($"User with ID {userId} not found.");

        user.FamilyIds.Add(familyId);

        await _users.UpdateAsync(user, cancellationToken);
    }
}
