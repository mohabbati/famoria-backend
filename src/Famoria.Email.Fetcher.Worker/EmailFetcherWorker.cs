using Famoria.Domain.Enums;
using Famoria.Application.Features.FetchEmails;
using Famoria.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Famoria.Email.Fetcher.Worker;

public class EmailFetcherWorker : BackgroundService
{
    private readonly ILogger<EmailFetcherWorker> _logger;
    private readonly IMediator _mediator;
    private readonly IConnectorService _connectorService;
    private readonly IAesCryptoService _cryptoService;
    private readonly TimeSpan _fetchInterval;

    public EmailFetcherWorker(
        ILogger<EmailFetcherWorker> logger,
        IMediator mediator,
        IConnectorService connectorService,
        IAesCryptoService cryptoService,
        TimeSpan? fetchInterval = null)
    {
        _logger = logger;
        _mediator = mediator;
        _connectorService = connectorService;
        _cryptoService = cryptoService;
        _fetchInterval = fetchInterval ?? TimeSpan.FromHours(1);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var provider in Enum.GetValues<IntegrationProvider>())
            {
                _logger.LogInformation("Processing {Provider} linked accounts", provider);
                var linkedAccounts = await _connectorService.GetByAsync(provider, cancellationToken);

                foreach (var account in linkedAccounts)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    try
                    {
                        var decryptedAccessToken = _cryptoService.Decrypt(account.AccessToken);
                        var command = new FetchEmailsCommand(
                            account.FamilyId,
                            account.LinkedAccount,
                            decryptedAccessToken,
                            account.LastFetchedAtUtc);
                        var processedCount = await _mediator.Send(command, cancellationToken);
                        _logger.LogInformation(
                            "Fetched {Count} emails for {Email} from {Provider} at {Time}",
                            processedCount,
                            account.LinkedAccount,
                            provider,
                            DateTimeOffset.Now);
                        if (processedCount > 0)
                        {
                            await _connectorService.UpdateLastFetchedAsync(
                                provider,
                                account.LinkedAccount,
                                DateTime.UtcNow,
                                cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing linked account {Email} from {Provider}", account.LinkedAccount, provider);
                    }
                }
            }

            await Task.Delay(_fetchInterval, cancellationToken);
        }
    }
}
