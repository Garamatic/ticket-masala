using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Web.Models;

public class AuditLogEntry
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TicketId { get; set; }
    [ForeignKey("TicketId")]
    public Ticket? Ticket { get; set; }

    [Required]
    public required string Action { get; set; } // e.g., "Created", "Updated", "StatusChanged"

    public string? PropertyName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public string? UserId { get; set; }
    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
