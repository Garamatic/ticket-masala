using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// A short, Twitter-style knowledge snippet optimized for FTS5 search.
/// Replaces the traditional "Article" with atomic units of knowledge.
/// </summary>
public class KnowledgeBaseSnippet
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    public string Tags { get; set; } = string.Empty; // Hashtags extracted from content

    public int UsageCount { get; set; } = 0; // MasalaRank Factor
    public bool IsVerified { get; set; } = false; // MasalaRank Factor

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? AuthorId { get; set; }
    public virtual ApplicationUser? Author { get; set; }
}
