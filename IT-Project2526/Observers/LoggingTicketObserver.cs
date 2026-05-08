using IT_Project2526.Models;
using IT_Project2526.RabbitMQ.RabbitMQDTOs;
using RabbitMqConnector;
using Xunit.Sdk;
using static System.Net.Mime.MediaTypeNames;

namespace IT_Project2526.Observers;

/// <summary>
/// Observer for logging ticket lifecycle events.
/// Provides audit trail and debugging information.
/// </summary>
public class LoggingTicketObserver : ITicketObserver
{
    private readonly ILogger<LoggingTicketObserver> _logger;
    private readonly MsgQ _msgQ;
    public LoggingTicketObserver(ILogger<LoggingTicketObserver> logger,MsgQ msgQ)
    {
        _logger = logger;
        _msgQ = msgQ;
    }

    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        _logger.LogInformation(
            "Ticket Created - ID: {TicketGuid}, Customer: {CustomerId}, Description: {Description}",
            ticket.Guid,
            ticket.CustomerId,
            ticket.Description.Length > 50 ? ticket.Description.Substring(0, 50) + "..." : ticket.Description);

        var ticketCreatedDto = new TicketCreatedDTO
        {
            TicketId = ticket.Guid,
            CustomerEmail = ticket.Customer?.Email ?? "unknown@customer.com",
            CustomerName = $"{ticket.Customer?.FirstName} {ticket.Customer?.LastName}".Trim(),
            TenantId = ticket.ProjectGuid?.ToString() ?? "default-tenant",
            Description = ticket.Description,
            Priority = ticket.PriorityScore > 10 ? "high" : "medium",
            CreatedAt = ticket.CreationDate
        };
        await _msgQ.SendMessage<TicketCreatedDTO>(ticketCreatedDto, RoutingKeys.Ticket);
        await Task.CompletedTask;
    }

    public async Task OnTicketAssignedAsync(Ticket ticket, Employee assignee)
    {
        _logger.LogInformation(
            "Ticket Assigned - ID: {TicketGuid}, Agent: {AgentName}, Team: {Team}",
            ticket.Guid,
            $"{assignee.FirstName} {assignee.LastName}",
            assignee.Team);

        var ticketAssignedDto = new TicketAssignedDTO
        {
            TicketId = ticket.Guid,
            AssignedTo = assignee.Id.ToString(),
            AssignedBy = ticket.ResponsibleId ?? "System",
            AssignedAt = DateTimeOffset.UtcNow
        };

        await _msgQ.SendMessage<TicketAssignedDTO>(ticketAssignedDto, RoutingKeys.Ticket);
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

        var ticketResolvedDto = new TicketResolvedDTO
        {
            TicketId = ticket.Guid,
            CustomerEmail = ticket.Customer?.Email ?? "unknown@customer.com",
            CustomerName = $"{ticket.Customer?.FirstName} {ticket.Customer?.LastName}".Trim(),
            ServiceDescription = ticket.Project?.Name ?? ticket.Description,
            ResolvedAt = ticket.CompletionDate ?? DateTime.UtcNow,
            TenantId = ticket.ProjectGuid?.ToString() ?? "default-tenant",
            Amount = 0m
        };
        await _msgQ.SendMessage<TicketResolvedDTO>(ticketResolvedDto, RoutingKeys.Ticket);
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
