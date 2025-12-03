using IT_Project2526.Models;
using IT_Project2526.Services.GERDA;

namespace IT_Project2526.Observers;

/// <summary>
/// Observer that triggers GERDA AI processing when tickets are created or updated.
/// Implements automatic AI-driven ticket processing without manual intervention.
/// </summary>
public class GerdaTicketObserver : ITicketObserver
{
    private readonly IGerdaService _gerdaService;
    private readonly ILogger<GerdaTicketObserver> _logger;

    public GerdaTicketObserver(IGerdaService gerdaService, ILogger<GerdaTicketObserver> logger)
    {
        _gerdaService = gerdaService;
        _logger = logger;
    }

    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        try
        {
            _logger.LogInformation("GERDA Observer: Processing new ticket {TicketGuid}", ticket.Guid);
            await _gerdaService.ProcessTicketAsync(ticket.Guid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GERDA Observer: Failed to process ticket {TicketGuid}", ticket.Guid);
            // Don't throw - observers should not break the main workflow
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
}
