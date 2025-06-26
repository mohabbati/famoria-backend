using Famoria.Application.Features.FetchEmails;
using Microsoft.Extensions.Logging;

namespace Famoria.Application.Features.ProcessLinkedAccounts;

public class ProcessLinkedAccountsHandler : IRequestHandler<ProcessLinkedAccountsCommand, int>
{
    private readonly ILogger<ProcessLinkedAccountsHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IConnectorService _connectorService;
    private readonly IAesCryptoService _cryptoService;

    public ProcessLinkedAccountsHandler(
        ILogger<ProcessLinkedAccountsHandler> logger,
        IMediator mediator,
        IConnectorService connectorService,
        IAesCryptoService cryptoService)
    {
        _logger = logger;
        _mediator = mediator;
        _connectorService = connectorService;
        _cryptoService = cryptoService;
    }

    public async Task<int> Handle(ProcessLinkedAccountsCommand request, CancellationToken cancellationToken)
    {
        var provider = request.Provider;
        int totalProcessed = 0;
        //try
        //{
            _logger.LogInformation("Processing {Provider} linked accounts", provider);
            var linkedAccounts = await _connectorService.GetByAsync(provider, cancellationToken);

            foreach (var account in linkedAccounts)
            {
                if (cancellationToken.IsCancellationRequested) break;
                //try
                //{
                    string decryptedAccessToken = _cryptoService.Decrypt(account.RefreshToken!);
                    var command = new FetchEmailsCommand(
                        FamilyId: account.FamilyId,
                        UserEmail: account.LinkedAccount,
                        AccessToken: decryptedAccessToken,
                        Since: account.LastFetchedAtUtc
                    );
                    var processedCount = await _mediator.Send(command, cancellationToken);
                    totalProcessed += processedCount;
                    _logger.LogInformation("Fetched {Count} emails for {Email} from {Provider} at {Time}",
                        processedCount, account.LinkedAccount, provider, DateTimeOffset.Now);
                    if (processedCount > 0)
                    {
                        await _connectorService.UpdateLastFetchedAsync(
                            provider,
                            account.LinkedAccount,
                            DateTime.UtcNow,
                            cancellationToken);
                    }
                //}
                //catch (Exception ex)
                //{
                //    _logger.LogError(ex, "Error processing linked account {Email} from {Provider}",
                //        account.LinkedAccount, provider);
                //}
            }
            _logger.LogInformation("Completed fetching from {Provider}. Total emails processed: {Count}",
                provider, totalProcessed);
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogError(ex, "Error processing {Provider} linked accounts", provider);
        //}
        return totalProcessed;
    }
}
