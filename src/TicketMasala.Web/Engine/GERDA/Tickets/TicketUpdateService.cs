using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets.Domain;
using TicketMasala.Web.Engine.Security;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service responsible for updating existing tickets.
/// Handles PII scrubbing, persistence, notifications, and audit logging.
/// </summary>
public interface ITicketUpdateService
{
    /// <summary>
    /// Updates an existing ticket.
    /// </summary>
    /// <param name="ticket">The ticket to update (should already be validated for edit permissions)</param>
    /// <param name="updatedByUserId">User performing the update (for audit)</param>
    /// <returns>True if update succeeded, false if it failed</returns>
    Task<bool> UpdateAsync(Ticket ticket, string? updatedByUserId);
}

/// <summary>
/// Implementation of ticket update workflow.
/// </summary>
internal class TicketUpdateService : ITicketUpdateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPiiScrubberService _piiScrubber;
    private readonly IEnumerable<ITicketObserver> _observers;
    private readonly Domain.TicketNotificationService _ticketNotificationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<TicketUpdateService> _logger;

    public TicketUpdateService(
        IUnitOfWork unitOfWork,
        IPiiScrubberService piiScrubber,
        IEnumerable<ITicketObserver> observers,
        Domain.TicketNotificationService ticketNotificationService,
        IAuditService auditService,
        ILogger<TicketUpdateService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _piiScrubber = piiScrubber ?? throw new ArgumentNullException(nameof(piiScrubber));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
        _ticketNotificationService = ticketNotificationService ?? throw new ArgumentNullException(nameof(ticketNotificationService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> UpdateAsync(Ticket ticket, string? updatedByUserId)
    {
        try
        {
            // PII Scrubbing
            ticket.Description = _piiScrubber.Scrub(ticket.Description);

            // Note: Authorization and transition validation is expected to be done
            // by the caller (e.g., orchestrator/module) using domain methods
            // (ticket.ValidateCanEdit, ticket.ValidateCanChangeStatus) before calling this service.

            // Queue ticket update (not yet committed)
            await _unitOfWork.Tickets.UpdateAsync(ticket);

            // Audit trail (also queued)
            await _auditService.LogActionAsync(ticket.Guid, "Updated", updatedByUserId);

            // Commit all changes in a single transaction
            await _unitOfWork.CommitAsync();

            // Notify observers (after commit to ensure data is persisted)
            await NotifyObserversAsync(ticket);

            // Delegate notification logic for status changes
            await _ticketNotificationService.NotifyStatusChangeAsync(ticket);

            _logger.LogInformation("Ticket {TicketGuid} updated by {UserId}", ticket.Guid, updatedByUserId ?? "(system)");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ticket {TicketGuid}", ticket.Guid);
            return false;
        }
    }

    private async Task NotifyObserversAsync(Ticket ticket)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketUpdatedAsync(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Observer {ObserverType} failed during ticket update",
                    observer.GetType().Name);
            }
        }
    }
}
