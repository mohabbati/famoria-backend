using System.Threading.Channels;

namespace Famoria.Api.Background;

// REMARK: The background queue processes jobs serially (one at a time).
// If multiple users link their Gmail accounts simultaneously, jobs will be queued and processed in order.
// For higher concurrency or faster user feedback (e.g., if users are waiting to see emails),
// consider implementing parallel job processing or multiple background workers.
public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem, CancellationToken cancellationToken);
    ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue;

    public BackgroundTaskQueue(int capacity = 100)
    {
        _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(capacity);
    }

    public async ValueTask QueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem, CancellationToken cancellationToken)
    {
        if (workItem == null) throw new ArgumentNullException(nameof(workItem));
        await _queue.Writer.WriteAsync(workItem, cancellationToken);
    }

    public async ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);
        return workItem;
    }
}
