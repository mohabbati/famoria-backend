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
                { Name = userDto.Name, FirstName = userDto.FirstName, LastName = userDto.LastName };

        await _users.AddAsync(user, cancellationToken);

        return new FamoriaUserDto(
            user.Id,
            user.Name,
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
            user.Name,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Provider,
            user.ProviderSub,
            user.FamilyIds);
    }
    public async Task<FamoriaUserDto> AddFamilyToUserAsync(string userId, string familyId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByAsync(new FamoriaUser(userId), cancellationToken);

        if (user is null)
            throw new InvalidOperationException($"User with ID {userId} not found.");

        user.FamilyIds.Add(familyId);

        var updated = await _users.UpdateAsync(user, cancellationToken);

        return new FamoriaUserDto(
            updated.Id,
            updated.Name,
            updated.FirstName,
            updated.LastName,
            updated.Email,
            updated.Provider,
            updated.ProviderSub,
            updated.FamilyIds);
    }
}
