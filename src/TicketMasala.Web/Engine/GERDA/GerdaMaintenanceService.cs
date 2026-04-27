using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Background service for GERDA maintenance tasks.
/// Uses the deep IGerda interface for batch operations.
/// </summary>
internal sealed class GerdaMaintenanceService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ISystemClock _clock;
    private readonly ILogger<GerdaMaintenanceService> _logger;

    private readonly TimeSpan _batchProcessInterval = TimeSpan.FromHours(6);
    private readonly TimeSpan _anticipationCheckInterval = TimeSpan.FromHours(12);
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public GerdaMaintenanceService(
        IServiceScopeFactory serviceScopeFactory,
        ISystemClock clock,
        ILogger<GerdaMaintenanceService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GERDA Maintenance Service started");

        var lastBatchProcess = _clock.UtcNow.AddHours(-6); // Start soon after startup
        var lastAnticipationCheck = _clock.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = _clock.UtcNow;

                // Batch processing of open tickets (every 6 hours)
                if (now - lastBatchProcess >= _batchProcessInterval)
                {
                    await ProcessAllOpenTickets(stoppingToken);
                    lastBatchProcess = now;
                }

                // Capacity anticipation check (every 12 hours)
                if (now - lastAnticipationCheck >= _anticipationCheckInterval)
                {
                    await CheckCapacityRisk(stoppingToken);
                    lastAnticipationCheck = now;
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GERDA Maintenance Service main loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("GERDA Maintenance Service stopped");
    }

    /// <summary>
    /// Process all open tickets through GERDA pipeline.
    /// </summary>
    private async Task ProcessAllOpenTickets(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var gerda = scope.ServiceProvider.GetRequiredService<IGerda>();
        var ticketRepo = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GerdaMaintenanceService>>();

        if (!gerda.IsActive)
        {
            logger.LogDebug("GERDA is inactive, skipping batch processing");
            return;
        }

        try
        {
            logger.LogInformation("GERDA Maintenance: Starting batch processing of open tickets");

            var activeTickets = await ticketRepo.GetActiveTicketsAsync();
            var openTicketIds = activeTickets.Select(t => t.Guid).ToList();

            logger.LogInformation("GERDA Maintenance: Found {Count} open tickets to process", openTicketIds.Count);

            var processed = 0;
            var failed = 0;

            foreach (var ticketGuid in openTicketIds)
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    await gerda.ProcessAsync(ticketGuid);
                    processed++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GERDA Maintenance: Failed to process ticket {TicketGuid}", ticketGuid);
                    failed++;
                }

                // Brief yield between tickets to avoid overwhelming the system
                if (processed % 10 == 0)
                {
                    await Task.Delay(100, stoppingToken);
                }
            }

            logger.LogInformation(
                "GERDA Maintenance: Batch processing completed. Processed: {Processed}, Failed: {Failed}",
                processed, failed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GERDA Maintenance: Fatal error during batch processing");
        }
    }

    /// <summary>
    /// Check capacity risk using anticipation engine (if available).
    /// </summary>
    private async Task CheckCapacityRisk(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var anticipation = scope.ServiceProvider.GetService<IAnticipationEngine>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GerdaMaintenanceService>>();

        if (anticipation?.IsEnabled != true)
        {
            logger.LogDebug("Anticipation engine not available, skipping capacity check");
            return;
        }

        try
        {
            logger.LogInformation("GERDA Maintenance: Checking capacity risk");

            var risk = await anticipation.CheckCapacityRiskAsync();

            if (risk != null)
            {
                logger.LogWarning(
                    "GERDA-A: Capacity risk detected! {Message} (Risk: {Percentage}%)",
                    risk.AlertMessage, risk.RiskPercentage);
            }
            else
            {
                logger.LogInformation("GERDA Maintenance: No capacity risk detected");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GERDA Maintenance: Error during capacity risk check");
        }
    }
}
