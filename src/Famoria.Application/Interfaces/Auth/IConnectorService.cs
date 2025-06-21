using System.Security.Claims;

namespace Famoria.Application.Interfaces;

public interface IConnectorService
{
    Task LinkAsync(string provider, string familyId, ClaimsPrincipal principal, string accessToken, string? refreshToken, DateTime expiresAt, CancellationToken cancellationToken);
}
