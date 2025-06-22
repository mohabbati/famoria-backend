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
        //TODO: Check for existing linked account for the same user and provider

        var encryptedAccessToken = _crypto.Encrypt(accessToken);
        var encryptedRefreshToken = refreshToken is null ? null : _crypto.Encrypt(refreshToken);

        var linkedAccount = new UserLinkedAccount
        {
            FamilyId = familyId,
            UserId = famoriaUserId,
            Provider = provider,
            Source = FamilyItemSource.Email,
            UserEmail = email,
            AccessToken = encryptedAccessToken,
            RefreshToken = encryptedRefreshToken,
            TokenExpiresAtUtc = expiresAt,
            IsActive = true
        };

        await _repository.UpsertAsync(linkedAccount, cancellationToken);
    }
}
