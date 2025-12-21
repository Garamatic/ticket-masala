using System.ComponentModel.DataAnnotations;
using TicketMasala.Domain.Common;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a saved filter for ticket searches.
/// </summary>
public class SavedFilter
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    public string? UserId { get; set; }

    // Stored as JSON or individual fields? Individual fields are easier to query/map back to ViewModel
    public string? SearchTerm { get; set; }
    public Status? Status { get; set; }
    public TicketType? TicketType { get; set; }
    public Guid? ProjectId { get; set; }
    public string? AssignedToId { get; set; }
    public string? CustomerId { get; set; }
    public bool? IsOverdue { get; set; }
    public bool? IsDueSoon { get; set; }
}

