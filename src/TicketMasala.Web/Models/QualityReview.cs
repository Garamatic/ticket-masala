using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Web.Models;

public class QualityReview
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TicketId { get; set; }

    [ForeignKey("TicketId")]
    public Ticket? Ticket { get; set; }

    [Required]
    public string ReviewerId { get; set; } = string.Empty;

    [ForeignKey("ReviewerId")]
    public ApplicationUser? Reviewer { get; set; }

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