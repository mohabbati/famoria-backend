namespace Famoria.Api.Identity;

public class SignInService : ISignInService
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;

    public SignInService(IUserService userService, IJwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    public async Task<string> SignInAsync(FamoriaUserDto userDto, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByAsync(userDto.Id, cancellationToken);
        
        if (user is null)
        {
            user = await _userService.CreateAsync(userDto, cancellationToken);
        }

        var token = await _jwtService.SignAsync(user);

        return token;
    }
}
