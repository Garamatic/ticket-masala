using TicketMasala.Domain.Events;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Infrastructure.DomainEvents;

namespace TicketMasala.Web.Handlers.DomainEvents;

/// <summary>
/// Example domain event handler that logs ticket assignments for audit purposes.
/// </summary>
public class TicketAssignedLogHandler : IDomainEventHandler<TicketAssignedEvent>
{
    private readonly ILogger<TicketAssignedLogHandler> _logger;
    private readonly IAuditService _auditService;

    public TicketAssignedLogHandler(
        ILogger<TicketAssignedLogHandler> logger,
        IAuditService auditService)
    {
        _logger = logger;
        _auditService = auditService;
    }

    public async Task HandleAsync(TicketAssignedEvent @event, CancellationToken cancellationToken = default)
    {
        var action = @event.OldResponsibleId == null ? "Assigned" : "Reassigned";

        _logger.LogInformation(
            "Ticket {TicketGuid} {Action} from {OldResponsible} to {NewResponsible} by {AssignedBy}",
            @event.TicketGuid,
            action,
            @event.OldResponsibleId ?? TicketMasala.Domain.Entities.Ticket.UnassignedIndicator,
            @event.NewResponsibleId,
            @event.AssignedByUserId);

        // Create an audit log entry
        await _auditService.LogActionAsync(
            @event.TicketGuid,
            $"Ticket{action}",
            @event.AssignedByUserId,
            propertyName: "ResponsibleId",
            oldValue: @event.OldResponsibleId,
            newValue: @event.NewResponsibleId);
    }
}
