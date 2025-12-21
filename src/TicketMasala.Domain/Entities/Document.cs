using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a document/attachment associated with a ticket.
/// </summary>
public class Document
{
    [Key]
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    public string? UploaderId { get; set; }

    public Guid TicketId { get; set; }

    public bool IsPublic { get; set; } = false;
}

