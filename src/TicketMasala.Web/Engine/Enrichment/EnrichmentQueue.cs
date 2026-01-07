using System.Threading.Channels;

namespace TicketMasala.Web.Engine.Enrichment;

public interface IEnrichmentQueue
{
    ValueTask QueueEnrichmentAsync(EnrichmentWorkItem workItem);
    ValueTask<EnrichmentWorkItem> DequeueAsync(CancellationToken cancellationToken);
}

public class EnrichmentQueue : IEnrichmentQueue
{
    private readonly Channel<EnrichmentWorkItem> _queue;

    public EnrichmentQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<EnrichmentWorkItem>(options);
    }

    public async ValueTask QueueEnrichmentAsync(EnrichmentWorkItem workItem)
    {
        if (workItem == null) throw new ArgumentNullException(nameof(workItem));
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<EnrichmentWorkItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
