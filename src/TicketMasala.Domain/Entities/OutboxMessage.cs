using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a message in the Outbox pattern for reliable event publishing.
/// Messages are persisted in the same transaction as domain changes,
/// then drained to RabbitMQ by a background service.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier for the outbox message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of the event being published (e.g., "ticket.resolved").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// JSON payload of the event.
    /// </summary>
    [Required]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// RabbitMQ routing key for the event.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the message was successfully published (null if not yet published).
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Number of retry attempts for failed publishes.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Error message from the last failed publish attempt.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Scheduled time for the next retry attempt (null if not scheduled).
    /// </summary>
    public DateTime? ScheduledRetryAt { get; set; }
}
