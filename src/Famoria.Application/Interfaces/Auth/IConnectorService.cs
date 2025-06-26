namespace Famoria.Application.Interfaces;

public interface IConnectorService
{
    Task LinkAsync(string famoriaUserId, string familyId, IntegrationProvider provider, string email, string accessToken, string? refreshToken, DateTime expiresAt, CancellationToken cancellationToken);
    Task<IEnumerable<UserLinkedAccountDto>> GetByAsync(IntegrationProvider provider, CancellationToken cancellationToken);
    Task UpdateLastFetchedAsync(IntegrationProvider provider, string linkedAccount, DateTime fetchedAt, CancellationToken cancellationToken);
}
