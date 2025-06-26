using Famoria.Application.Features.ProcessLinkedAccounts;
using Famoria.Domain.Enums;
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
            //try
            //{
                using var scope = _scopeFactory.CreateScope();

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                foreach (var provider in Enum.GetValues<IntegrationProvider>())
                {
                    await mediator.Send(new ProcessLinkedAccountsCommand(provider), cancellationToken);
                }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error in email fetcher main loop.");
            //}
            await Task.Delay(_fetchInterval, cancellationToken);
        }
    }
}
