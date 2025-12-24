using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.GERDA.Tickets;

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
        private readonly IngestionQueue<IngestionRequest> _queue;
        private readonly IServiceScopeFactory _scopeFactory;

        public IngestionWorker(
            ILogger<IngestionWorker> logger, 
            IngestionQueue<IngestionRequest> queue,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _queue = queue;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Gatekeeper Ingestion Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var request = await _queue.DequeueAsync(stoppingToken);
                    _logger.LogInformation("Processing ingested request with template: {Template}", request.Template);

                    using var scope = _scopeFactory.CreateScope();
                    var templateService = scope.ServiceProvider.GetRequiredService<IIngestionTemplateService>();
                    var ticketService = scope.ServiceProvider.GetService<ITicketService>();

                    if (ticketService == null)
                    {
                        _logger.LogWarning("ITicketService is not registered in GatekeeperApi. Ingestion will only log results.");
                    }

                    var result = templateService.Transform(request.Template, request.Data);

                    if (result.Success)
                    {
                        _logger.LogInformation("Successfully transformed data for Domain: {DomainId}", result.DomainId);
                        
                        if (ticketService != null)
                        {
                            var ticket = await ticketService.CreateTicketAsync(
                                result.Description ?? "No description",
                                result.CustomerId ?? "system", // Use a system user or default if not provided
                                null, // No initial assignment
                                null, // No initial project
                                null  // Default completion target
                            );
                            
                            _logger.LogInformation("Created ticket {TicketGuid} from ingestion", ticket.Guid);
                        }
                    }
                    else
                    {
                        _logger.LogError("Transformation failed: {Error}", result.Error);
                    }
                }
                catch (OperationCanceledException)
                {
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
