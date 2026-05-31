using TicketMasala.Domain.Ports;

namespace TicketMasala.Domain.Events;

/// <summary>
/// Wraps a typed domain event with transport-level metadata used by the
/// infrastructure layer when publishing to a message broker.
///
/// The envelope follows the "Context + Payload" pattern: it carries everything
/// a consumer needs to correctly route, deserialize, and process the event
/// without coupling the domain model to broker-specific concepts.
///
/// Usage in domain code:
/// <code>
/// var envelope = DomainEventEnvelope.Create(ticketCreatedEvent, metadata);
/// await _publisher.PublishAsync(envelope, cancellationToken);
/// </code>
/// </summary>
public sealed class DomainEventEnvelope<TEvent> : IDomainEventEnvelope
    where TEvent : IDomainEvent
{
    /// <summary>
    /// The actual domain event being published.
    /// </summary>
    public TEvent Event { get; }

    /// <summary>
    /// Unique identifier used for deduplication by consumers (copy of <see cref="IDomainEvent.EventId"/>).
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// UTC timestamp of when the event originally occurred (copy of <see cref="IDomainEvent.OccurredOn"/>).
    /// </summary>
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Optional correlation ID linking this event to a parent operation (e.g., command or workflow).
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Optional causation ID pointing to the immediate cause of this event.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Tenant identifier for multi-tenant routing and enforcement.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Logical name of the event type (e.g., "ticket-created", "ticket-assigned").
    /// Derived from the event type name.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Schema version of the event body.
    /// </summary>
    public string SchemaVersion { get; init; } = "1";

    /// <summary>
    /// Explicit routing key override. When set, overrides the default routing key
    /// convention derived from <see cref="EventType"/>.
    /// The default convention is: "event.{eventType}" → "event.ticket-created".
    /// Override to dot-separated format: "event.ticket.created".
    /// </summary>
    public string? RoutingKeyOverride { get; init; }

    object IDomainEventEnvelope.Payload => Event;

    private DomainEventEnvelope(TEvent @event, string eventType)
    {
        Event = @event;
        EventId = @event.EventId;
        OccurredOn = @event.OccurredOn;
        EventType = eventType;
    }

    /// <summary>
    /// Creates an envelope from a domain event with inferred metadata.
    /// </summary>
    public static DomainEventEnvelope<TEvent> Create(TEvent @event) =>
        new(@event, InferEventTypeName(@event));

    /// <summary>
    /// Creates an envelope with additional metadata supplied by the caller.
    /// </summary>
    public static DomainEventEnvelope<TEvent> Create(
        TEvent @event,
        EventMetadata metadata) => new(
            @event,
            InferEventTypeName(@event))
        {
            CorrelationId = metadata.CorrelationId,
            CausationId = metadata.CausationId,
            TenantId = metadata.TenantId,
            SchemaVersion = metadata.SchemaVersion
        };

    /// <summary>
    /// Infers the event type name from the CLR type name.
    /// Conventions:
    ///   "TicketCreatedEvent" → "ticket-created"
    ///   "TicketResolvedEvent" → "ticket-resolved"
    /// </summary>
    private static string InferEventTypeName<T>(T @event) where T : IDomainEvent
    {
        var name = @event.GetType().Name;
        // Strip "Event" suffix
        if (name.EndsWith("Event", StringComparison.Ordinal))
            name = name[..^5];
        // Convert PascalCase to kebab-case: "TicketCreated" → "ticket-created"
        return ConvertPascalToKebab(name);
    }

    private static string ConvertPascalToKebab(string pascalCase)
    {
        return string.Concat(
            pascalCase.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "-" + c.ToString().ToLowerInvariant()
                    : c.ToString().ToLowerInvariant()));
    }

    // ─── Equality ─────────────────────────────────────────────────────────────

    public bool Equals(DomainEventEnvelope<TEvent>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EventId == other.EventId;
    }

    public override bool Equals(object? obj) =>
        obj is DomainEventEnvelope<TEvent> envelope && Equals(envelope);

    public override int GetHashCode() => EventId.GetHashCode();

    public void Deconstruct(
        out TEvent @event,
        out Guid eventId,
        out string eventType,
        out string? correlationId,
        out string? causationId,
        out string? tenantId)
    {
        @event = Event;
        eventId = EventId;
        eventType = EventType;
        correlationId = CorrelationId;
        causationId = CausationId;
        tenantId = TenantId;
    }
}

/// <summary>
/// Transport-independent metadata passed when creating a <see cref="DomainEventEnvelope{TEvent}"/>.
/// Populated by the application or domain service layer that has command/context IDs.
/// </summary>
public sealed record EventMetadata
{
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public string? TenantId { get; init; }
    public string SchemaVersion { get; init; } = "1";
}
