namespace Famoria.Api.Background;

public class QueuedHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(IBackgroundTaskQueue taskQueue, IServiceProvider serviceProvider, ILogger<QueuedHostedService> logger)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);
            try
            {
                using var scope = _serviceProvider.CreateScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing background job in QueuedHostedService.");
            }
        }
    }
}
