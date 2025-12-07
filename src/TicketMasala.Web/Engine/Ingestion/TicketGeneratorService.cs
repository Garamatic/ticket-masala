using TicketMasala.Web.ViewModels.Ingestion;
using System.Threading.Channels;

namespace TicketMasala.Web.Engine.Ingestion;

public class TicketGeneratorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TicketGeneratorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _interval;
    private readonly bool _enabled;
    private readonly Channel<IngestionWorkItem> _channel;

    public TicketGeneratorService(
        IServiceProvider serviceProvider,
        ILogger<TicketGeneratorService> logger,
        IConfiguration configuration,
        Channel<IngestionWorkItem> channel)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _channel = channel;
        
        _enabled = _configuration.GetValue<bool>("TicketGenerator:Enabled");
        var intervalSeconds = _configuration.GetValue<int>("TicketGenerator:IntervalSeconds", 60);
        _interval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Ticket Generator is disabled.");
            return;
        }

        _logger.LogInformation("Ticket Generator started. Interval: {Interval}", _interval);

        await foreach (var workItem in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await GenerateRandomTicketAsync(workItem, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random ticket");
            }
        }
    }

    private async Task GenerateRandomTicketAsync(IngestionWorkItem workItem, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var ticketGenerator = scope.ServiceProvider.GetRequiredService<ITicketGenerator>();
        
        await ticketGenerator.GenerateRandomTicketAsync(stoppingToken);
    }

}
