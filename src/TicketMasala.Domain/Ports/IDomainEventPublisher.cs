using TicketMasala.Domain.Events;

namespace TicketMasala.Domain.Ports;

/// <summary>
/// Non-generic view of a domain event envelope used by the publisher port.
/// Avoids the generic invariance problem (envelope of TicketCreatedEvent is
/// not an envelope of IDomainEvent).
/// </summary>
public interface IDomainEventEnvelope
{
    /// <summary>
    /// Unique identifier used for deduplication by consumers.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// UTC timestamp of when the event originally occurred.
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Optional correlation ID for distributed tracing.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Optional causation ID pointing to the command that triggered this event.
    /// </summary>
    string? CausationId { get; }

    /// <summary>
    /// Tenant identifier for multi-tenant routing.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Logical event type name (e.g. "ticket-created", "ticket-resolved").
    /// Used for routing and consumer dispatch.
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Schema version of the event body.
    /// </summary>
    string SchemaVersion { get; }

    /// <summary>
    /// Explicit routing key override. When set, the publisher uses this value
    /// instead of the default routing key convention.
    /// </summary>
    string? RoutingKeyOverride { get; }

    /// <summary>
    /// The payload to serialize (either the domain event or an integration contract).
    /// </summary>
    object Payload { get; }
}

/// <summary>
/// Domain port for publishing domain events to an external message broker.
/// Implementations live in the infrastructure layer.
///
/// The domain uses this interface to express the intent to publish without
/// depending on any transport-specific library.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a single domain event. The implementation handles serialization,
    /// routing, and acknowledgement (e.g. via the transactional outbox pattern).
    /// </summary>
    Task PublishAsync(
        IDomainEventEnvelope domainEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a batch of domain events in the order they were raised.
    /// </summary>
    Task PublishAsync(
        IEnumerable<IDomainEventEnvelope> domainEvents,
        CancellationToken cancellationToken = default);
}
