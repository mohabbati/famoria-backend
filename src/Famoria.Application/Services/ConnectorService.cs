using Famoria.Domain.Common;
using Famoria.Domain.Enums;

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

    public async Task LinkAsync(string famoriaUserId, string familyId, string provider, string email, string accessToken, string? refreshToken, DateTime expiresAt, CancellationToken cancellationToken)
    {
        var encryptedAccessToken = _crypto.Encrypt(accessToken);
        var encryptedRefreshToken = refreshToken is null ? null : _crypto.Encrypt(refreshToken);

        var id = IdFactory.CreateDeterministicId($"{famoriaUserId}:{FamilyItemSource.Email}:{email}");

        var linkedAccount = new UserLinkedAccount
        {
            Id = id,
            FamilyId = familyId,
            UserId = famoriaUserId,
            Provider = provider,
            Source = FamilyItemSource.Email,
            LinkedAccount = email,
            AccessToken = encryptedAccessToken,
            RefreshToken = encryptedRefreshToken,
            TokenExpiresAtUtc = expiresAt,
            IsActive = true
        };

        await _repository.UpsertAsync(linkedAccount, cancellationToken);
    }
}
