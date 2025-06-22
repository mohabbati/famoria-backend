namespace Famoria.Application.Interfaces;

public interface IConnectorService
{
    Task LinkAsync(string famoriaUserId, string familyId, string provider, string email, string accessToken, string? refreshToken, DateTime expiresAt, CancellationToken cancellationToken);
}
