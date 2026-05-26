using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Exceptions;
using TicketMasala.Domain.Tenancy;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Messaging;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

using IntegrationEvent = TicketMasala.Web.Messaging.Events.TicketResolvedEvent;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service responsible for ticket resolution operations.
/// Handles the complete resolution workflow: domain resolution, persistence,
/// audit logging, observer notification, and event publishing.
/// </summary>
/// <remarks>
/// Extracted from TicketWorkflowService to follow Single Responsibility Principle.
/// This is the canonical path for resolving tickets; prefer this over direct
/// repository mutation or ad-hoc resolution logic.
/// </remarks>
[Obsolete("Use ITicketLifecycle and command records instead. This interface will be removed in a future release.", false)]
public interface ITicketResolutionService
{
    /// <summary>
    /// Resolves a ticket with the specified resolution details.
    /// </summary>
    /// <param name="ticketGuid">The ticket to resolve</param>
    /// <param name="resolutionNotes">Required notes about the resolution</param>
    /// <param name="billableAmount">Optional billable amount for invoicing</param>
    /// <param name="resolvedByUserId">The user performing the resolution</param>
    /// <returns>True if resolution succeeded, false if ticket not found or invalid state</returns>
    /// <exception cref="DomainException">Thrown if resolution notes are invalid (handled internally, logged)</exception>
    Task<bool> ResolveAsync(
        Guid ticketGuid,
        string resolutionNotes,
        decimal? billableAmount,
        string resolvedByUserId);
}

/// <summary>
/// Implementation of ticket resolution workflow.
/// </summary>
internal class TicketResolutionService : ITicketResolutionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IEnumerable<ITicketObserver> _observers;
    private readonly IRabbitMqPublisher? _rabbitMqPublisher;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TicketResolutionService> _logger;

    public TicketResolutionService(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IEnumerable<ITicketObserver> observers,
        IRabbitMqPublisher? rabbitMqPublisher,
        ITenantContext? tenantContext,
        ILogger<TicketResolutionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
        _rabbitMqPublisher = rabbitMqPublisher; // Optional - can be null if RabbitMQ disabled
        _tenantContext = tenantContext ?? new DefaultTenantContext();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ResolveAsync(
        Guid ticketGuid,
        string resolutionNotes,
        decimal? billableAmount,
        string resolvedByUserId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketGuid, includeRelations: true);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found for resolution", ticketGuid);
            return false;
        }

        // Perform domain resolution
        try
        {
            ticket.Resolve(resolutionNotes, billableAmount, resolvedByUserId);
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Cannot resolve ticket {TicketGuid}: {Reason}", ticketGuid, ex.Message);
            return false;
        }

        // Queue ticket update (not yet committed)
        await _unitOfWork.Tickets.UpdateAsync(ticket);

        // Audit trail (also queued)
        await _auditService.LogActionAsync(
            ticketGuid,
            "Resolved",
            resolvedByUserId,
            "Ticket",
            null,
            resolutionNotes);

        // Commit all changes in a single transaction
        await _unitOfWork.CommitAsync();

        // Notify observers (sync side effects - after commit to ensure data is persisted)
        await NotifyObserversAsync(ticket);

        // Publish integration event (if RabbitMQ available)
        await PublishIntegrationEventAsync(ticket, resolutionNotes);

        _logger.LogInformation(
            "Ticket {TicketGuid} resolved by {UserId}. Billable: {Amount}",
            ticketGuid,
            resolvedByUserId,
            billableAmount);

        return true;
    }

    private async Task NotifyObserversAsync(Ticket ticket)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketUpdatedAsync(ticket);
                await observer.OnTicketCompletedAsync(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Observer {ObserverType} failed during ticket resolution",
                    observer.GetType().Name);
            }
        }
    }

    private async Task PublishIntegrationEventAsync(Ticket ticket, string originalResolutionNotes)
    {
        if (_rabbitMqPublisher == null)
            return;

        try
        {
            var evt = new IntegrationEvent
            {
                TicketId = ticket.Guid.ToString(),
                CustomerEmail = ticket.Customer?.Email ?? string.Empty,
                CustomerName = $"{ticket.Customer?.FirstName} {ticket.Customer?.LastName}".Trim(),
                ServiceDescription = ticket.Title,
                Amount = ticket.BillableAmount ?? 0m,
                TenantId = _tenantContext.TenantId ?? string.Empty,
                ResolvedAt = ticket.CompletionDate ?? DateTime.UtcNow,
                ResolutionNotes = ticket.ResolutionNotes ?? originalResolutionNotes
            };

            await _rabbitMqPublisher.PublishAsync(evt, "ticket.resolved");

            _logger.LogDebug(
                "Published ticket.resolved event for {TicketId}",
                ticket.Guid);
        }
        catch (Exception ex)
        {
            // Log but don't fail the resolution - domain event is already persisted in outbox
            _logger.LogError(
                ex,
                "Failed to publish ticket.resolved event for {TicketId}. " +
                "Domain event is persisted in outbox for retry.",
                ticket.Guid);
        }
    }
}
