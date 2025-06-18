using Famoria.Application.Models.Dtos;

namespace Famoria.Application.Interfaces;

public interface ISignInService
{
    Task<string> SignInAsync(FamoriaUserDto userDto, CancellationToken cancellationToken = default);
}
