using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.Portal;

public class PortalSubmissionViewModel
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [EmailAddress]
    public string? CustomerEmail { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerPhone { get; set; }

    public string? WorkItemType { get; set; }

    // Custom fields as JSON string
    public Dictionary<string, string>? CustomFields { get; set; }

    // File upload support
    public IFormFile? Attachment { get; set; }

    // Geolocation support
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Priority/urgency
    public int? PriorityScore { get; set; }

    // Tags for AI processing
    public string? Tags { get; set; }
}

public class PortalSubmissionResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? TicketGuid { get; set; }
    public string? TicketNumber { get; set; }
}
