using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GatekeeperApi
{
    public class IngestionQueue<T>
    {
        private readonly Channel<T> _queue;

        public IngestionQueue()
        {
            _queue = Channel.CreateUnbounded<T>();
        }

        public async ValueTask EnqueueAsync(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            await _queue.Writer.WriteAsync(item);
        }

        public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }

    public class IngestionWorker : BackgroundService
    {
        private readonly ILogger<IngestionWorker> _logger;
        private readonly IngestionQueue<string> _queue;

        public IngestionWorker(ILogger<IngestionWorker> logger, IngestionQueue<string> queue)
        {
            _logger = logger;
            _queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ingestion Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var payload = await _queue.DequeueAsync(stoppingToken);
                    _logger.LogInformation("Processing ingested payload: {Payload}", payload);
                    // Placeholder for actual processing logic
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing ingestion payload");
                }
            }
        }
    }
}
