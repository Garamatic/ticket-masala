using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a comment on a ticket.
/// </summary>
public class TicketComment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TicketId { get; set; }
    public virtual Ticket? Ticket { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;

    public string? AuthorId { get; set; }
    public virtual ApplicationUser? Author { get; set; }

    public bool IsInternal { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

