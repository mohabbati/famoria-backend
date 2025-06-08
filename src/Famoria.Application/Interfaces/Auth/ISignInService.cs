using System.Security.Claims;

namespace Famoria.Application.Interfaces;
public interface ISignInService
{
    Task<string> SignInAsync(string provider, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
