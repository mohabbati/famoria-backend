namespace Famoria.Summarizer.Worker;

public class SummarizerWorker : BackgroundService
{
    private readonly ILogger<SummarizerWorker> _logger;

    public SummarizerWorker(ILogger<SummarizerWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
