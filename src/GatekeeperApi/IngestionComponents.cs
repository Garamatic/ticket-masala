using System.Threading.Channels;

namespace GatekeeperApi;

/// <summary>
/// Thread-safe bounded queue for ingestion requests using System.Threading.Channels.
/// Prevents memory exhaustion by limiting the number of queued items.
/// </summary>
public class IngestionQueue
{
    private readonly Channel<IngestionRequest> _queue;

    public IngestionQueue(int capacity = 10000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        };
        _queue = Channel.CreateBounded<IngestionRequest>(options);
    }

    public bool TryEnqueue(IngestionRequest item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        return _queue.Writer.TryWrite(item);
    }

    public async ValueTask<IngestionRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}

/// <summary>
/// Background worker that processes ingestion requests.
/// In a full microservices architecture, this would publish to a message bus.
/// In standalone mode, it logs and stores for later processing.
/// </summary>
public class IngestionWorker : BackgroundService
{
    private readonly ILogger<IngestionWorker> _logger;
    private readonly IngestionQueue _queue;

    public IngestionWorker(
        ILogger<IngestionWorker> logger,
        IngestionQueue queue)
    {
        _logger = logger;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Gatekeeper Ingestion Worker started. Ready to accept requests.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await _queue.DequeueAsync(stoppingToken);

                _logger.LogInformation(
                    "Ingestion request dequeued: Template={Template}, Keys={KeyCount}",
                    request.Template,
                    request.Data.Count);

                // In a full microservices deployment, this would:
                // 1. Publish to message bus (RabbitMQ, Azure Service Bus, etc.)
                // 2. Or call TicketMasala.Web API via HTTP client
                // 3. Or process locally if ITicketWorkflowService is registered via plugin
                //
                // For now, requests are simply dequeued and logged. Actual processing
                // depends on the deployment configuration.
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Ingestion worker stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ingestion request");
            }
        }
    }
}
