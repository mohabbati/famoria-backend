using Microsoft.Extensions.Logging;

namespace Famoria.Application.Features.FetchEmails;

public class FetchEmailsHandler : IRequestHandler<FetchEmailsCommand, int>
{
    private readonly IEmailFetcher _emailFetcher;
    private readonly IEmailService _emailPersistenceService;
    private readonly IAesCryptoService _cryptoService;
    private readonly IConnectorService _connectorService;
    private readonly ILogger<FetchEmailsHandler> _logger;

    public FetchEmailsHandler(IEmailFetcher emailFetcher, IEmailService emailPersistenceService, IAesCryptoService cryptoService, IConnectorService connectorService, ILogger<FetchEmailsHandler> logger)
    {
        _emailFetcher = emailFetcher;
        _emailPersistenceService = emailPersistenceService;
        _cryptoService = cryptoService;
        _connectorService = connectorService;
        _logger = logger;
    }

    public async Task<int> Handle(FetchEmailsCommand request, CancellationToken cancellationToken)
    {
        int successCount = 0;

        foreach (var account in request.LinkedAccounts)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                var decryptedAccessToken = _cryptoService.Decrypt(account.AccessToken);

                var emails = await _emailFetcher.GetNewEmailsAsync(account.LinkedAccount, decryptedAccessToken, account.LastFetchedAtUtc, cancellationToken);
                int emailSuccessCount = 0;
                foreach (var eml in emails)
                {
                    try
                    {
                        await _emailPersistenceService.PersistAsync(eml, account.FamilyId, cancellationToken);
                        emailSuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist email for family {FamilyId}", account.FamilyId);
                    }
                }

                if (emailSuccessCount > 0 || request.ForceUpdateLastFetched)
                {
                    await _connectorService.UpdateLastFetchedAsync(
                        account.Provider,
                        account.LinkedAccount,
                        DateTime.UtcNow,
                        cancellationToken);
                }

                successCount++;

                _logger.LogInformation(
                    "Fetched {Count} emails for {Email} from {Provider} at {Time}",
                    emailSuccessCount,
                    account.LinkedAccount,
                    account.Provider,
                    DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing linked account {Email} from {Provider}", account.LinkedAccount, account.Provider);
            }
        }

        return successCount;
    }
}
