using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a notification for a user.
/// </summary>
public class Notification
{
    [Key]
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    [StringLength(500)]
    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string Type { get; set; } = "Info"; // Info, Warning, Success, Error
}

