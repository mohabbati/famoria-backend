namespace Famoria.Api.Identity;

public interface ISignInService
{
    Task<string> SignInAsync(FamoriaUserDto userDto, CancellationToken cancellationToken = default);
}
