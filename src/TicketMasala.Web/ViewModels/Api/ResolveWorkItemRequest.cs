using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.Api;

/// <summary>
/// Request DTO for resolving a work item (ticket).
/// </summary>
public class ResolveWorkItemRequest
{
    /// <summary>
    /// Notes about how the work item was resolved (required).
    /// </summary>
    [Required]
    [StringLength(2000, ErrorMessage = "Resolution notes cannot exceed 2000 characters")]
    public string ResolutionNotes { get; set; } = string.Empty;

    /// <summary>
    /// Optional billable amount for invoicing purposes.
    /// </summary>
    [Range(0, 100000, ErrorMessage = "Billable amount must be between 0 and 100,000")]
    public decimal? BillableAmount { get; set; }
}
