using System.Threading.Channels;

namespace GatekeeperApi;

/// <summary>
/// Thread-safe queue for ingestion requests using System.Threading.Channels.
/// </summary>
public class IngestionQueue<T>
{
    private readonly Channel<T> _queue;

    public IngestionQueue()
    {
        _queue = Channel.CreateUnbounded<T>();
    }

    public async ValueTask EnqueueAsync(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        await _queue.Writer.WriteAsync(item);
    }

    public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
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
    private readonly IngestionQueue<IngestionRequest> _queue;

    public IngestionWorker(
        ILogger<IngestionWorker> logger,
        IngestionQueue<IngestionRequest> queue)
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
                    "Queued ingestion request: Template={Template}, Keys={Keys}",
                    request.Template,
                    string.Join(", ", request.Data.Keys));

                // In a full microservices deployment:
                // 1. Publish to message bus (RabbitMQ, Azure Service Bus, etc.)
                // 2. Or call TicketMasala.Web API via HTTP client
                // 3. Or process locally if ITicketWorkflowService is registered via plugin

                // For now, the request is accepted and logged. The actual processing
                // depends on how the service is deployed.
                _logger.LogInformation("Ingestion request accepted and logged for template: {Template}", request.Template);
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
