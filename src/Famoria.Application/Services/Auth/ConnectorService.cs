using Famoria.Domain.Common;

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

    public async Task LinkAsync(string famoriaUserId, string familyId, IntegrationProvider provider, string email, string accessToken, string? refreshToken, DateTime expiresAt, CancellationToken cancellationToken)
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

    public async Task<IEnumerable<UserLinkedAccountDto>> GetByAsync(IntegrationProvider provider, CancellationToken cancellationToken)
    {
        var result = await _repository.GetAsync(
            x => true/*x.Provider.ToString() == provider.ToString() && x.IsActive == true*/,
            cancellationToken: cancellationToken
        );

        var linkedAccounts = result.ToList();

        var defaultLastFetchedAt = DateTime.UtcNow.AddDays(-7);

        return result.Select(x => new UserLinkedAccountDto(
            x.FamilyId,
            x.LinkedAccount,
            x.AccessToken,
            x.RefreshToken,
            x.LastFetchedAtUtc ?? defaultLastFetchedAt));
    }

    public async Task UpdateLastFetchedAsync(IntegrationProvider provider, string linkedAccount, DateTime fetchedAt, CancellationToken cancellationToken)
    {
        // Find the account by provider and email
        var accounts = await _repository.GetAsync(
            x => true/*x.Provider == provider && x.LinkedAccount == linkedAccount && x.IsActive*/,
            cancellationToken: cancellationToken);
        
        var account = accounts.FirstOrDefault();
        if (account == null)
        {
            throw new InvalidOperationException($"No active linked account found for {linkedAccount} with provider {provider}");
        }
        
        // Update the LastFetchedAtUtc property
        account.LastFetchedAtUtc = fetchedAt;
        
        // Save the changes
        await _repository.UpsertAsync(account, cancellationToken);
    }
}
