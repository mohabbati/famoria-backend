using Famoria.Application.Models.Dtos;

namespace Famoria.Application.Interfaces;

public interface IUserService
{
    Task<string> SignInAsync(FamoriaUserDto userDto, CancellationToken cancellationToken = default);
    Task AddFamilyToUserAsync(string userId, string familyId, CancellationToken cancellationToken = default);
}
