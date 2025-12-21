using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.Api;

/// <summary>
/// Domain-agnostic DTO for WorkContainer (maps to Project entity).
/// Provides a consistent API interface for project/container management.
/// </summary>
public class WorkContainerDto
{
    /// <summary>
    /// Unique identifier for the work container
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the work container
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the work container
    /// </summary>
    [StringLength(5000)]
    public string? Description { get; set; }

    /// <summary>
    /// Current status of the work container
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// ID of the manager responsible for this work container
    /// </summary>
    public string? ManagerId { get; set; }

    /// <summary>
    /// When the work container was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Target completion date for the work container
    /// </summary>
    public DateTime? CompletionTarget { get; set; }

    /// <summary>
    /// When the work container was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Number of work items currently in this container
    /// </summary>
    public int WorkItemCount { get; set; }

    /// <summary>
    /// Type of the work container (e.g., "Development", "Support")
    /// </summary>
    public string? ProjectType { get; set; }

    /// <summary>
    /// Additional notes about the work container
    /// </summary>
    [StringLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Customer ID associated with this work container
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// List of customer IDs associated with this work container
    /// </summary>
    public List<string> CustomerIds { get; set; } = new List<string>();

    /// <summary>
    /// AI-generated roadmap for this work container
    /// </summary>
    [StringLength(10000)]
    public string? AiRoadmap { get; set; }
}