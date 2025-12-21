using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents an audit log entry for tracking changes to tickets.
/// </summary>
public class AuditLogEntry
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TicketId { get; set; }

    public string Action { get; set; } = string.Empty; // e.g., "Created", "Updated", "StatusChanged"

    public string? PropertyName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

