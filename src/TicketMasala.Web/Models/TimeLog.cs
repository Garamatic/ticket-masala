using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Web.Models;

public class TimeLog : BaseModel
{
    [Required]
    public Guid TicketId { get; set; }

    [ForeignKey("TicketId")]
    public Ticket? Ticket { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    [Range(0.1, 24.0, ErrorMessage = "Hours must be between 0.1 and 24")]
    public double Hours { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
