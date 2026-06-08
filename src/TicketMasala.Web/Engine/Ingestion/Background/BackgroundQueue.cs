using System.Threading.Channels;

namespace TicketMasala.Web.Engine.Ingestion.Background;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
    int QueuedCount { get; }
    long DroppedCount { get; }
}

public class BackgroundQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;
    private long _droppedCount;

    public BackgroundQueue(int capacity)
    {
        // Capacity should be set based on expected load and available memory
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public int QueuedCount => _queue.Reader.Count;
    public long DroppedCount => Interlocked.Read(ref _droppedCount);

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        if (_queue.Writer.TryWrite(workItem))
            return;

        Interlocked.Increment(ref _droppedCount);
        // Non-blocking: drop the item when the queue is full to prevent HTTP request stalls
        await ValueTask.CompletedTask;
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);
        return workItem;
    }
}
