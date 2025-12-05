using IT_Project2526.Models;
using IT_Project2526.Services;
using IT_Project2526.Services.GERDA;
using Microsoft.Extensions.DependencyInjection;

namespace IT_Project2526.Observers;

/// <summary>
/// Observer that triggers GERDA AI processing when tickets are created or updated.
/// Implements automatic AI-driven ticket processing without manual intervention.
/// </summary>
public class GerdaTicketObserver : ITicketObserver
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<GerdaTicketObserver> _logger;

    public GerdaTicketObserver(
        IBackgroundTaskQueue taskQueue, 
        IServiceScopeFactory serviceScopeFactory,
        ILogger<GerdaTicketObserver> logger)
    {
        _taskQueue = taskQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        try
        {
            _logger.LogInformation("GERDA Observer: Queueing ticket {TicketGuid} for background processing", ticket.Guid);
            
            await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var gerdaService = scope.ServiceProvider.GetRequiredService<IGerdaService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<GerdaTicketObserver>>();
                    
                    try 
                    {
                        logger.LogInformation("GERDA Background: Processing ticket {TicketGuid}", ticket.Guid);
                        await gerdaService.ProcessTicketAsync(ticket.Guid);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "GERDA Background: Failed to process ticket {TicketGuid}", ticket.Guid);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GERDA Observer: Failed to queue ticket {TicketGuid}", ticket.Guid);
        }
    }

    public async Task OnTicketAssignedAsync(Ticket ticket, Employee assignee)
    {
        try
        {
            _logger.LogInformation(
                "GERDA Observer: Ticket {TicketGuid} assigned to {AgentName}",
                ticket.Guid,
                $"{assignee.FirstName} {assignee.LastName}");
            
            // Could update training data here for future recommendations
            // For now, just log the assignment
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GERDA Observer: Failed to handle assignment for ticket {TicketGuid}", ticket.Guid);
        }
        
        await Task.CompletedTask;
    }

    public async Task OnTicketCompletedAsync(Ticket ticket)
    {
        try
        {
            _logger.LogInformation("GERDA Observer: Ticket {TicketGuid} completed", ticket.Guid);
            
            // Could trigger model retraining here if needed
            // For now, just log completion
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GERDA Observer: Failed to handle completion for ticket {TicketGuid}", ticket.Guid);
        }
        
        await Task.CompletedTask;
    }

    public async Task OnTicketUpdatedAsync(Ticket ticket)
    {
        try
        {
            _logger.LogDebug("GERDA Observer: Ticket {TicketGuid} updated", ticket.Guid);
            // General updates don't need special handling yet
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GERDA Observer: Failed to handle update for ticket {TicketGuid}", ticket.Guid);
        }
        
        await Task.CompletedTask;
    }

    public async Task OnTicketCommentedAsync(TicketComment comment)
    {
        // GERDA currently doesn't react to comments, but could analyze sentiment here
        await Task.CompletedTask;
    }
}
