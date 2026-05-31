using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Events;
using TicketMasala.Domain.Ports;

namespace TicketMasala.Web.Infrastructure.DomainEvents;

/// <summary>
/// Infrastructure adapter that implements <see cref="IDomainEventPublisher"/> by persisting
/// <see cref="IDomainEventEnvelope"/> as <see cref="OutboxMessage"/> rows
/// directly in the current <see cref="MasalaDbContext"/>.
///
/// This keeps the domain port decoupled from RabbitMQ while still ensuring reliable,
/// at-least-once delivery via the outbox pattern. The OutboxPublisher background
/// service drains outbox rows and publishes them to RabbitMQ.
///
/// IMPORTANT: This publisher should be used INSIDE the interceptor's SavingChangesAsync
/// to ensure outbox rows are committed atomically with the aggregate changes.
/// </summary>
public sealed class DomainEventPublisher : IDomainEventPublisher
{
    // JSON options matching the rest of the outbox pipeline (snake_case, no nulls)
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Maps kebab-case event types to backwards-compatible dotted routing keys.
    // See the already-published events from TicketLifecycleService for the pattern.
    private static readonly Dictionary<string, string> RoutingKeyConventions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ticket-created"] = "event.ticket.created",
        ["ticket-assigned"] = "event.ticket.assigned",
        ["ticket-resolved"] = "event.ticket.resolved",
        ["ticket-grouped"] = "event.ticket.grouped",
        ["ticket-ungrouped"] = "event.ticket.ungrouped",
        ["ticket-updated"] = "event.ticket.updated",
        ["ticket-status-changed"] = "event.ticket.status_changed",
    };

    private readonly MasalaDbContext _context;
    private readonly ILogger<DomainEventPublisher> _logger;

    public DomainEventPublisher(MasalaDbContext context, ILogger<DomainEventPublisher> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MasalaDbContext Context => _context;

    /// <inheritdoc />
    public async Task PublishAsync(
        IDomainEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var outboxMessage = CreateOutboxMessage(envelope);
        _context.OutboxMessages.Add(outboxMessage);
        await Task.CompletedTask; // Keep interface async for future implementations

        _logger.LogDebug(
            "Enqueued domain event {EventType} (EventId: {EventId}) as outbox message {MessageId}",
            envelope.EventType,
            envelope.EventId,
            outboxMessage.Id);
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        IEnumerable<IDomainEventEnvelope> envelopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        var list = envelopes.ToList();

        foreach (var envelope in list)
        {
            var outboxMessage = CreateOutboxMessage(envelope);
            _context.OutboxMessages.Add(outboxMessage);
        }

        await Task.CompletedTask;

        _logger.LogDebug(
            "Enqueued {Count} domain events as outbox messages",
            list.Count);
    }

    /// <summary>
    /// Converts an <see cref="IDomainEventEnvelope"/> into an <see cref="OutboxMessage"/>
    /// ready for persistence. The caller is responsible for adding the message
    /// to the appropriate DbContext (e.g. context.OutboxMessages.Add).
    /// </summary>
    public static OutboxMessage CreateOutboxMessage(IDomainEventEnvelope envelope)
    {
        // Serialize the event payload.
        //   If a contract mapper replaced envelope.Event (via DomainEventEnvelopeWithPayloadOverride),
        //   we serialize the integration contract; otherwise the raw domain event.
        var payload = JsonSerializer.Serialize(envelope.Payload, JsonOptions);

        return new OutboxMessage
        {
            Id = envelope.EventId,
            EventType = envelope.EventType,
            Payload = payload,
            RoutingKey = ResolveRoutingKey(envelope),
            CorrelationId = envelope.CorrelationId ?? Activity.Current?.Id,
            CreatedAt = envelope.OccurredOn
        };
    }

    /// <summary>
    /// Resolves a RabbitMQ routing key from the envelope metadata.
    /// Uses <see cref="IDomainEventEnvelope.RoutingKeyOverride"/> when set,
    /// otherwise falls back to a configured convention dictionary that maps
    /// event types to backwards-compatible dotted routing keys.
    /// </summary>
    private static string ResolveRoutingKey(IDomainEventEnvelope envelope)
    {
        // 1. Explicit override wins
        if (envelope.RoutingKeyOverride is not null)
            return envelope.RoutingKeyOverride;

        // 2. Check the conventions dictionary
        if (RoutingKeyConventions.TryGetValue(envelope.EventType, out var routingKey))
            return routingKey;

        // 3. Fallback: event.{eventType} (e.g. "event.ticket-created")
        return $"event.{envelope.EventType}";
    }
}
