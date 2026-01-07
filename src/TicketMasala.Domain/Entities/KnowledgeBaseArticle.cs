using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a knowledge base article.
/// </summary>
public class KnowledgeBaseArticle
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string Tags { get; set; } = string.Empty; // Comma separated tags

    public int UsageCount { get; set; } = 0;
    public bool IsVerified { get; set; } = false;

    public string? AuthorId { get; set; }
    public virtual ApplicationUser? Author { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

