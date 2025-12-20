using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Web.Models;

public class Notification
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public required string UserId { get; set; }
    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    [StringLength(500)]
    public required string Message { get; set; }

    [StringLength(500)]
    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string Type { get; set; } = "Info"; // Info, Warning, Success, Error
}
