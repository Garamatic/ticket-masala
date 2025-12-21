using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a quality review for a ticket.
/// </summary>
public class QualityReview
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TicketId { get; set; }

    [Required]
    public string ReviewerId { get; set; } = string.Empty;
    public virtual ApplicationUser? Reviewer { get; set; }

    [Required]
    [MaxLength(5000)]
    public string Comments { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string? Feedback { get; set; }

    [Range(0, 100)]
    public int Score { get; set; }

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
}

