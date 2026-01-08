using TicketMasala.Domain.Data;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Engine.Enrichment;

public class EnrichmentBackgroundService : BackgroundService
{
    private readonly ILogger<EnrichmentBackgroundService> _logger;
    private readonly IEnrichmentQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrichmentBackgroundService(
        ILogger<EnrichmentBackgroundService> logger,
        IEnrichmentQueue queue,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _queue = queue;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Enrichment Background Service is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _queue.DequeueAsync(stoppingToken);
                await ProcessWorkItemAsync(workItem, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing enrichment work item.");
            }
        }
    }

    private async Task ProcessWorkItemAsync(EnrichmentWorkItem workItem, CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
        
        var ticket = await context.Tickets.FindAsync(new object[] { workItem.TicketId }, token);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found for enrichment.", workItem.TicketId);
            return;
        }

        _logger.LogInformation("Enriching Ticket {TicketId} ({Type})...", workItem.TicketId, workItem.EnrichmentType);
        
        if (workItem.EnrichmentType == "All" || workItem.EnrichmentType == "Sentiment")
        {
            // Use existing SimpleSentimentAnalyzer
            var (score, label) = TicketMasala.Web.Engine.GERDA.Sentiment.SimpleSentimentAnalyzer.Analyze(ticket.Title, ticket.Description);
            
            // Append tag
            var sentimentTag = $"Sentiment:{label}";
            if (string.IsNullOrEmpty(ticket.GerdaTags))
            {
                ticket.GerdaTags = sentimentTag;
            }
            else if (!ticket.GerdaTags.Contains("Sentiment:"))
            {
                ticket.GerdaTags += $",{sentimentTag}";
            }
            
            // Adjust priority if critical (simple boost)
            if (label == "Critical" || label == "High")
            {
                ticket.PriorityScore = Math.Max(ticket.PriorityScore, score * 10);
            }
            
            _logger.LogInformation("Sentiment Analysis for Ticket {TicketId}: {Label} (Score: {Score})", workItem.TicketId, label, score);
        }

        await context.SaveChangesAsync(token);
        _logger.LogInformation("Enrichment complete for Ticket {TicketId}.", workItem.TicketId);

        // Auto-Dispatch
        if (workItem.EnrichmentType == "All" || workItem.EnrichmentType == "Dispatch")
        {
             try 
             {
                 var dispatchService = scope.ServiceProvider.GetRequiredService<IDispatchingService>();
                 await dispatchService.AutoDispatchTicketAsync(workItem.TicketId);
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Auto-dispatch failed for ticket {TicketId}", workItem.TicketId);
             }
        }
    }
}
