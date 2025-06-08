using Famoria.Domain.Enums;
using System.Security.Claims;

namespace Famoria.Application.Services;

public class ConnectorService : IConnectorService
{
    private readonly IAesCryptoService _crypto;
    private readonly IRepository<UserLinkedAccount> _repository;

    public ConnectorService(IAesCryptoService crypto, IRepository<UserLinkedAccount> repository)
    {
        _crypto = crypto;
        _repository = repository;
    }

    public async Task LinkAsync(string provider, string familyId, ClaimsPrincipal principal, string accessToken, string? refreshToken, int expiresInSeconds, CancellationToken cancellationToken)
    {
        var userId = principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? 
                    principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? 
                    throw new InvalidOperationException("sub missing");
        var email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;

        var linkedAccount = new UserLinkedAccount
        {
            FamilyId = familyId,
            UserId = userId,
            Provider = provider,
            Source = FamilyItemSource.Email,
            UserEmail = email,
            AccessToken = _crypto.Encrypt(accessToken),
            RefreshToken = refreshToken is null ? null : _crypto.Encrypt(refreshToken),
            TokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds),
            IsActive = true
        };
        await _repository.UpsertAsync(linkedAccount, cancellationToken);
    }
}
