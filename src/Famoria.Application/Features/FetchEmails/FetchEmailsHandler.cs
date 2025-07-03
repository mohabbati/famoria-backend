using Microsoft.Extensions.Logging;

namespace Famoria.Application.Features.FetchEmails;

public class FetchEmailsHandler : IRequestHandler<FetchEmailsCommand, int>
{
    private readonly IEmailFetcher _emailFetcher;
    private readonly IEmailPersistenceService _emailPersistenceService;
    private readonly IAesCryptoService _cryptoService;
    private readonly IConnectorService _connectorService;
    private readonly ILogger<FetchEmailsHandler> _logger;

    public FetchEmailsHandler(IEmailFetcher emailFetcher, IEmailPersistenceService emailPersistenceService, IAesCryptoService cryptoService, IConnectorService connectorService, ILogger<FetchEmailsHandler> logger)
    {
        _emailFetcher = emailFetcher;
        _emailPersistenceService = emailPersistenceService;
        _cryptoService = cryptoService;
        _connectorService = connectorService;
        _logger = logger;
    }

    public async Task<int> Handle(FetchEmailsCommand request, CancellationToken cancellationToken)
    {
        var decryptedAccessToken = _cryptoService.Decrypt(request.AccessToken);

        // Use the 'Since' value from the command
        var emails = await _emailFetcher.GetNewEmailsAsync(request.UserEmail, decryptedAccessToken, request.Since, cancellationToken);
        int successCount = 0;
        foreach (var eml in emails)
        {
            try
            {
                await _emailPersistenceService.PersistAsync(eml, request.FamilyId, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist email for family {FamilyId}", request.FamilyId);
            }
        }

        if (successCount > 0 || request.ForceUpdateLastFetched)
        {
            await _connectorService.UpdateLastFetchedAsync(
                request.Provider,
                request.UserEmail,
                DateTime.UtcNow,
                cancellationToken);
        }

        return successCount;
    }
}
