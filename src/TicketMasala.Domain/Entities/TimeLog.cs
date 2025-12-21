using System.ComponentModel.DataAnnotations;
using TicketMasala.Domain.Common;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents time logged against a ticket.
/// </summary>
public class TimeLog : BaseModel
{
    [Required]
    public Guid TicketId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Range(0.1, 24.0, ErrorMessage = "Hours must be between 0.1 and 24")]
    public double Hours { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}

