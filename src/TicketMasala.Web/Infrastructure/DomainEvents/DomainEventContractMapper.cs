using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Events;
using TicketMasala.Domain.Tenancy;

namespace TicketMasala.Web.Infrastructure.DomainEvents;

/// <summary>
/// Result of a successful domain-event-to-integration mapping.
/// </summary>
public sealed record IntegrationEventMapping
{
    /// <summary>The object to serialize into the outbox payload.</summary>
    public required object Payload { get; init; }

    /// <summary>Integration event type, e.g. "ticket.created".</summary>
    public required string EventType { get; init; }

    /// <summary>RabbitMQ routing key, e.g. "event.ticket.created".</summary>
    public required string RoutingKey { get; init; }
}

/// <summary>
/// Maps a domain event to an integration contract for outbox serialization.
/// Implementations are discovered and called by DomainEventDispatchingInterceptor
/// during SaveChangesAsync.
///
/// When no mapper is registered for an event type, the event is skipped (no outbox row).
/// </summary>
public interface IDomainEventContractMapper
{
    /// <summary>
    /// Returns the integration mapping, or <c>null</c> if this mapper
    /// does not handle the given event.
    /// </summary>
    IntegrationEventMapping? Map(IDomainEvent @event);
}

/// <summary>
/// Maps <see cref="TicketCreatedEvent"/> to <see cref="RabbitMqConnector.Contracts.TicketCreatedEvent"/>.
/// Enriches from the tracked <see cref="Ticket"/> entity in the current DbContext.
/// </summary>
public sealed class TicketCreatedContractMapper : IDomainEventContractMapper
{
    private readonly MasalaDbContext _context;
    private readonly ITenantContext _tenantContext;

    public TicketCreatedContractMapper(MasalaDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public IntegrationEventMapping? Map(IDomainEvent @event)
    {
        if (@event is not TicketCreatedEvent created)
            return null;

        var ticket = _context.ChangeTracker.Entries<Domain.Entities.Ticket>()
            .FirstOrDefault(e => e.Entity.Guid == created.TicketGuid)?.Entity;

        var customerEmail = string.Empty;
        var customerName = string.Empty;
        var description = string.Empty;
        var priority = "medium";

        if (ticket is not null)
        {
            description = ticket.Title;
            priority = MapPriorityScore(ticket.PriorityScore);

            // Customer is often a navigation property — try to resolve it
            if (ticket.Customer is not null)
            {
                customerEmail = ticket.Customer.Email ?? string.Empty;
                customerName = $"{ticket.Customer.FirstName} {ticket.Customer.LastName}".Trim();
            }
        }

        // Fallback: look up the customer from the DbContext
        if (string.IsNullOrEmpty(customerEmail) && !string.IsNullOrEmpty(created.CustomerId))
        {
            var customer = _context.Users.Find(created.CustomerId);
            if (customer is not null)
            {
                customerEmail = customer.Email ?? string.Empty;
                customerName = $"{customer.FirstName} {customer.LastName}".Trim();
            }
        }

        return new IntegrationEventMapping
        {
            Payload = new RabbitMqConnector.Contracts.TicketCreatedEvent
            {
                Source = "ticket-masala",
                TicketId = created.TicketGuid.ToString(),
                CustomerEmail = customerEmail,
                CustomerName = customerName,
                TenantId = _tenantContext.TenantId ?? string.Empty,
                Description = description,
                Priority = priority,
                CreatedAt = created.OccurredOn.ToString("O"),
                EventId = created.EventId.ToString()
            },
            EventType = "ticket.created",
            RoutingKey = "event.ticket.created"
        };
    }

    private static string MapPriorityScore(double score) => score switch
    {
        < 3 => "low",
        < 7 => "medium",
        _ => "high"
    };
}

/// <summary>
/// Maps <see cref="TicketAssignedEvent"/> to <see cref="RabbitMqConnector.Contracts.TicketAssignedEvent"/>.
/// </summary>
public sealed class TicketAssignedContractMapper : IDomainEventContractMapper
{
    private readonly MasalaDbContext _context;

    public TicketAssignedContractMapper(MasalaDbContext context)
    {
        _context = context;
    }

    public IntegrationEventMapping? Map(IDomainEvent @event)
    {
        if (@event is not TicketAssignedEvent assigned)
            return null;

        var ticket = _context.ChangeTracker.Entries<Domain.Entities.Ticket>()
            .FirstOrDefault(e => e.Entity.Guid == assigned.TicketGuid)?.Entity;

        var customerEmail = string.Empty;
        var customerName = string.Empty;

        if (ticket?.Customer is not null)
        {
            customerEmail = ticket.Customer.Email ?? string.Empty;
            customerName = $"{ticket.Customer.FirstName} {ticket.Customer.LastName}".Trim();
        }

        // Resolve assigned-to user name
        var assignedToId = assigned.NewResponsibleId;
        var assignedTo = _context.Users.Find(assignedToId);

        return new IntegrationEventMapping
        {
            Payload = new RabbitMqConnector.Contracts.TicketAssignedEvent
            {
                TicketId = assigned.TicketGuid.ToString(),
                CustomerEmail = customerEmail,
                CustomerName = customerName,
                AssignedTo = assignedTo?.FullName ?? assignedToId,
                AssignedBy = assigned.AssignedByUserId,
                AssignedAt = assigned.OccurredOn.ToString("O"),
                EventId = assigned.EventId.ToString()
            },
            EventType = "ticket.assigned",
            RoutingKey = "event.ticket.assigned"
        };
    }
}

/// <summary>
/// Maps <see cref="TicketResolvedEvent"/> to <see cref="RabbitMqConnector.Contracts.TicketResolvedEvent"/>.
/// </summary>
public sealed class TicketResolvedContractMapper : IDomainEventContractMapper
{
    private readonly MasalaDbContext _context;

    public TicketResolvedContractMapper(MasalaDbContext context)
    {
        _context = context;
    }

    public IntegrationEventMapping? Map(IDomainEvent @event)
    {
        if (@event is not TicketResolvedEvent resolved)
            return null;

        var ticket = _context.ChangeTracker.Entries<Domain.Entities.Ticket>()
            .FirstOrDefault(e => e.Entity.Guid == resolved.TicketGuid)?.Entity;

        var customerEmail = string.Empty;
        var customerName = string.Empty;
        var serviceDescription = string.Empty;

        if (ticket is not null)
        {
            serviceDescription = ticket.Title;
            if (ticket.Customer is not null)
            {
                customerEmail = ticket.Customer.Email ?? string.Empty;
                customerName = $"{ticket.Customer.FirstName} {ticket.Customer.LastName}".Trim();
            }
        }

        return new IntegrationEventMapping
        {
            Payload = new RabbitMqConnector.Contracts.TicketResolvedEvent
            {
                TicketId = resolved.TicketGuid.ToString(),
                CustomerEmail = customerEmail,
                CustomerName = customerName,
                ServiceDescription = serviceDescription,
                Amount = resolved.BillableAmount ?? 0m,
                TenantId = string.Empty,
                ResolvedAt = resolved.ResolvedAt.ToString("O"),
                ResolutionNotes = resolved.ResolutionNotes,
                EventId = resolved.EventId.ToString()
            },
            EventType = "ticket.resolved",
            RoutingKey = "event.ticket.resolved"
        };
    }
}
