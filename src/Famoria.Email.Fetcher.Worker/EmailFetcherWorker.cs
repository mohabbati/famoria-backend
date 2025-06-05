using Famoria.Application.Features.FetchEmails;

using MediatR;

namespace Famoria.Email.Fetcher.Worker;

public class EmailFetcherWorker : BackgroundService
{
    private readonly ILogger<EmailFetcherWorker> _logger;
    private readonly IMediator _mediator;
    private readonly TimeSpan _fetchInterval;

    public EmailFetcherWorker(ILogger<EmailFetcherWorker> logger, IMediator mediator, TimeSpan? fetchInterval = null)
    {
        _logger = logger;
        _mediator = mediator;
        _fetchInterval = fetchInterval ?? TimeSpan.FromHours(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Track the last fetch time
        DateTime lastFetch = DateTime.UtcNow - _fetchInterval;
        while (!stoppingToken.IsCancellationRequested)
        {
            var fetchWindowEnd = DateTime.UtcNow;
            try
            {
                // TODO: Replace with real values or configuration
                var command = new FetchEmailsCommand(
                    FamilyId: "demo-family",
                    UserEmail: "demo@gmail.com",
                    AccessToken: "demo-access-token",
                    Since: lastFetch
                );
                var processedCount = await _mediator.Send(command, stoppingToken);
                _logger.LogInformation("Fetched and persisted {Count} emails at {Time}", processedCount, DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running FetchEmailsHandler");
            }
            lastFetch = fetchWindowEnd;
            await Task.Delay(_fetchInterval, stoppingToken);
        }
    }
}
