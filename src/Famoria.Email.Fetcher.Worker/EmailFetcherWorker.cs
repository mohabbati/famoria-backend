using Famoria.Domain.Enums;
using Famoria.Application.Features.FetchEmails;
using Famoria.Application.Interfaces;
using MediatR;

namespace Famoria.Email.Fetcher.Worker;

public class EmailFetcherWorker : BackgroundService
{
    private readonly ILogger<EmailFetcherWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _fetchInterval = TimeSpan.FromHours(1);

    public EmailFetcherWorker(
        ILogger<EmailFetcherWorker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var connectorService = scope.ServiceProvider.GetRequiredService<IConnectorService>();
            var cryptoService = scope.ServiceProvider.GetRequiredService<IAesCryptoService>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            foreach (var provider in Enum.GetValues<IntegrationProvider>())
            {
                _logger.LogInformation("Processing {Provider} linked accounts", provider);
                var linkedAccounts = await connectorService.GetByAsync(provider, cancellationToken);

                foreach (var account in linkedAccounts)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    try
                    {
                        var decryptedAccessToken = cryptoService.Decrypt(account.AccessToken);
                        var command = new FetchEmailsCommand(
                            account.FamilyId,
                            account.LinkedAccount,
                            decryptedAccessToken,
                            account.LastFetchedAtUtc);
                        var processedCount = await mediator.Send(command, cancellationToken);
                        _logger.LogInformation(
                            "Fetched {Count} emails for {Email} from {Provider} at {Time}",
                            processedCount,
                            account.LinkedAccount,
                            provider,
                            DateTimeOffset.Now);
                        if (processedCount > 0)
                        {
                            await connectorService.UpdateLastFetchedAsync(
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
