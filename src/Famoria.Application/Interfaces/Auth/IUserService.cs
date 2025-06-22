namespace Famoria.Application.Interfaces;

public interface IUserService
{
    Task<FamoriaUserDto> CreateAsync(FamoriaUserDto userDto, CancellationToken cancellationToken = default);
    Task<FamoriaUserDto?> GetByAsync(string id, CancellationToken cancellationToken = default);
    Task<FamoriaUserDto> AddFamilyToUserAsync(string userId, string familyId, CancellationToken cancellationToken = default);
}
