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
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            foreach (var provider in Enum.GetValues<IntegrationProvider>())
            {
                // TODO: Skip providers that are not Google
                // For now, we only process Google accounts
                if (provider != IntegrationProvider.Google)
                    continue;

                var linkedAccounts = await connectorService.GetByAsync(provider, cancellationToken);

                if (linkedAccounts.Any())
                {
                    _logger.LogInformation(
                        "Starting email fetch for provider {Provider}. Found {Count} linked account(s) to process.",
                        provider, linkedAccounts.Count());

                    var processedCount = await mediator.Send(new FetchEmailsCommand(linkedAccounts), cancellationToken);

                    _logger.LogInformation(
                        "Completed email fetch for provider {Provider}. Successfully processed {Count} account(s).",
                        provider, processedCount);
                }
                else
                {
                    _logger.LogWarning("No linked accounts found for provider {Provider}. Skipping email fetch.", provider);
                }
            }

            await Task.Delay(_fetchInterval, cancellationToken);
        }
    }
}
