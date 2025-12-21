using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a notification for a user.
/// </summary>
public class Notification
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public required string UserId { get; set; }

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

