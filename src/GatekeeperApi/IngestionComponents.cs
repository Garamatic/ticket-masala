using System.Threading.Channels;

namespace GatekeeperApi;

/// <summary>
/// Thread-safe bounded queue for ingestion requests using System.Threading.Channels.
/// Prevents memory exhaustion by limiting the number of queued items.
/// </summary>
public class IngestionQueue<T>
{
    private readonly Channel<T> _queue;

    public IngestionQueue(IConfiguration config)
    {
        var capacity = config.GetValue<int>("Gatekeeper:QueueCapacity", 10000);
        _queue = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true, // We only have one IngestionWorker
            SingleWriter = false // Multiple HTTP requests will write
        });
    }

    public async ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        await _queue.Writer.WriteAsync(item, cancellationToken);
    }

    public bool TryEnqueue(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        return _queue.Writer.TryWrite(item);
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
    private readonly IServiceProvider _serviceProvider;

    public IngestionWorker(
        ILogger<IngestionWorker> logger,
        IngestionQueue<IngestionRequest> queue,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _queue = queue;
        _serviceProvider = serviceProvider;
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

                // Use a scope for the processor if it has scoped dependencies (like HttpClient from IHttpClientFactory)
                using (var scope = _serviceProvider.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<IIngestionProcessor>();
                    await processor.ProcessAsync(request, stoppingToken);
                }
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
