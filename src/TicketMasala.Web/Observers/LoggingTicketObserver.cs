using TicketMasala.Web.Models;

namespace TicketMasala.Web.Observers;

/// <summary>
/// Observer for logging ticket lifecycle events.
/// Provides audit trail and debugging information.
/// </summary>
public class LoggingTicketObserver : ITicketObserver
{
    private readonly ILogger<LoggingTicketObserver> _logger;

    public LoggingTicketObserver(ILogger<LoggingTicketObserver> logger)
    {
        _logger = logger;
    }

    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        _logger.LogInformation(
            "Ticket Created - ID: {TicketGuid}, Customer: {CustomerId}, Description: {Description}",
            ticket.Guid,
            ticket.CreatorGuid.ToString(),
            ticket.Description.Length > 50 ? ticket.Description.Substring(0, 50) + "..." : ticket.Description);
        
        await Task.CompletedTask;
    }

    public async Task OnTicketAssignedAsync(Ticket ticket, Employee assignee)
    {
        _logger.LogInformation(
            "Ticket Assigned - ID: {TicketGuid}, Agent: {AgentName}, Team: {Team}",
            ticket.Guid,
            $"{assignee.FirstName} {assignee.LastName}",
            assignee.Team);
        
        await Task.CompletedTask;
    }

    public async Task OnTicketCompletedAsync(Ticket ticket)
    {
        var resolutionTime = ticket.CompletionDate.HasValue
            ? (ticket.CompletionDate.Value - ticket.CreationDate).TotalHours
            : 0;

        _logger.LogInformation(
            "Ticket Completed - ID: {TicketGuid}, Resolution Time: {Hours:F1} hours, Status: {Status}",
            ticket.Guid,
            resolutionTime,
            ticket.TicketStatus);
        
        await Task.CompletedTask;
    }

    public Task OnTicketUpdatedAsync(Ticket ticket)
    {
        _logger.LogDebug(
            "Ticket Updated - ID: {TicketGuid}, Status: {Status}",
            ticket.Guid,
            ticket.TicketStatus);
        
        return Task.CompletedTask;
    }

    public Task OnTicketCommentedAsync(TicketComment comment)
    {
        _logger.LogInformation("Ticket {TicketGuid} commented by {AuthorId}", comment.TicketId, comment.AuthorId);
        return Task.CompletedTask;
    }

}
