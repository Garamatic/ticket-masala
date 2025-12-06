using TicketMasala.Web.Models;
using TicketMasala.Web.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web;

namespace TicketMasala.Web.Engine.Ingestion;

public class TicketGeneratorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TicketGeneratorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _interval;
    private readonly bool _enabled;

    public TicketGeneratorService(
        IServiceProvider serviceProvider,
        ILogger<TicketGeneratorService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
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

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await GenerateRandomTicketAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random ticket");
            }
        }
    }

    private async Task GenerateRandomTicketAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var ticketGenerator = scope.ServiceProvider.GetRequiredService<ITicketGenerator>();
        
        await ticketGenerator.GenerateRandomTicketAsync(stoppingToken);
    }

}
