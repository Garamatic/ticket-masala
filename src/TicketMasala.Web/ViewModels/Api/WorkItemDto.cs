using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.Api;

/// <summary>
/// Domain-agnostic DTO for WorkItem (maps to Ticket entity).
/// Provides a consistent API interface regardless of the underlying domain model.
/// </summary>
public class WorkItemDto
{
    /// <summary>
    /// Unique identifier for the work item
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Title/subject of the work item
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the work item
    /// </summary>
    [Required]
    [StringLength(5000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the work item
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// ID of the assigned handler (user responsible for the work item)
    /// </summary>
    public string? AssignedHandlerId { get; set; }

    /// <summary>
    /// ID of the work container (project) this item belongs to
    /// </summary>
    public Guid? ContainerId { get; set; }

    /// <summary>
    /// Domain identifier for this work item (e.g., "IT", "Legal")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string DomainId { get; set; } = string.Empty;

    /// <summary>
    /// Optional type code for domain-specific categorization
    /// </summary>
    [StringLength(50)]
    public string? TypeCode { get; set; }

    /// <summary>
    /// When the work item was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the work item was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Target completion date
    /// </summary>
    public DateTime? CompletionTarget { get; set; }

    /// <summary>
    /// Domain-specific custom fields as key-value pairs
    /// </summary>
    public Dictionary<string, object>? CustomFields { get; set; }

    /// <summary>
    /// AI-estimated effort points for this work item
    /// </summary>
    public int? EstimatedEffortPoints { get; set; }

    /// <summary>
    /// AI-calculated priority score
    /// </summary>
    public double? PriorityScore { get; set; }

    /// <summary>
    /// Customer ID associated with this work item
    /// </summary>
    public string? CustomerId { get; set; }
}