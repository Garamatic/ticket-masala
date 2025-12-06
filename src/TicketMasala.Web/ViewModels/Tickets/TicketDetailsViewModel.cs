using TicketMasala.Web.Models;

namespace TicketMasala.Web.ViewModels.Tickets;

/// <summary>
/// View model for ticket detail page with GERDA AI insights
/// </summary>
public class TicketDetailsViewModel
{
    // Core Ticket Information
    public Guid Guid { get; set; }
    public string Description { get; set; } = string.Empty;
    public Status TicketStatus { get; set; }
    public TicketType? TicketType { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? CompletionTarget { get; set; }
    public DateTime? CompletionDate { get; set; }
    
    // Relationships
    public string? CustomerName { get; set; }
    public string? CustomerId { get; set; }
    public string? ResponsibleName { get; set; }
    public string? ResponsibleId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? ProjectGuid { get; set; }
    
    // Domain Extensibility Fields
    public string? DomainId { get; set; }
    public string? WorkItemTypeCode { get; set; }
    public string? CustomFieldsJson { get; set; }
    
    // Comments
    public List<TicketComment> Comments { get; set; } = new();
    public List<Document> Attachments { get; set; } = new();
    public List<AuditLogEntry> AuditLogs { get; set; } = new();
    
    // Quality Review
    public List<QualityReview> QualityReviews { get; set; } = new();
    public ReviewStatus ReviewStatus { get; set; }
    
    // Sub-tickets
    public Guid? ParentTicketGuid { get; set; }
    public List<SubTicketInfo> SubTickets { get; set; } = new();
    
    // ============================================
    // GERDA AI Insights
    // ============================================
    
    /// <summary>
    /// Estimated effort points (from Estimating service)
    /// Fibonacci scale: 1, 2, 3, 5, 8, 13, 21
    /// </summary>
    public int EstimatedEffortPoints { get; set; }
    
    /// <summary>
    /// Human-readable complexity label
    /// </summary>
    public string ComplexityLabel => EstimatedEffortPoints switch
    {
        1 => "Trivial",
        2 or 3 => "Simple",
        5 => "Medium",
        8 => "Complex",
        13 or 21 => "Very Complex",
        _ => "Unknown"
    };
    
    /// <summary>
    /// WSJF priority score (from Ranking service)
    /// Higher = more urgent (Cost of Delay / Job Size)
    /// </summary>
    public double PriorityScore { get; set; }
    
    /// <summary>
    /// Human-readable urgency label
    /// </summary>
    public string UrgencyLabel => PriorityScore switch
    {
        >= 50 => "CRITICAL",
        >= 20 => "High",
        >= 5 => "Medium",
        _ => "Low"
    };
    
    /// <summary>
    /// CSS class for urgency badge styling
    /// </summary>
    public string UrgencyBadgeClass => UrgencyLabel switch
    {
        "CRITICAL" => "badge bg-danger",
        "High" => "badge bg-warning",
        "Medium" => "badge bg-info",
        _ => "badge bg-secondary"
    };
    
    /// <summary>
    /// AI-generated tags (comma-separated)
    /// e.g., "Password Reset, Urgent, First-Time User"
    /// </summary>
    public string? GerdaTags { get; set; }
    
    /// <summary>
    /// Parsed GERDA tags as list
    /// </summary>
    public List<string> GerdaTagsList => string.IsNullOrWhiteSpace(GerdaTags) 
        ? new List<string>() 
        : GerdaTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    
    /// <summary>
    /// Days until SLA breach (negative if already breached)
    /// </summary>
    public int DaysUntilSla => CompletionTarget.HasValue 
        ? (int)(CompletionTarget.Value - DateTime.UtcNow).TotalDays 
        : int.MaxValue;
    
    /// <summary>
    /// Is SLA already breached?
    /// </summary>
    public bool IsSlaBreached => CompletionTarget.HasValue && DateTime.UtcNow > CompletionTarget.Value;
    
    /// <summary>
    /// SLA status label
    /// </summary>
    public string SlaStatusLabel => IsSlaBreached 
        ? "BREACHED" 
        : DaysUntilSla <= 1 
            ? "Due Today" 
            : DaysUntilSla <= 3 
                ? $"{DaysUntilSla} days left" 
                : "On Track";
    
    /// <summary>
    /// Recommended agent from Dispatching service (if available)
    /// </summary>
    public RecommendedAgentInfo? RecommendedAgent { get; set; }
}

/// <summary>
/// Information about recommended agent from GERDA Dispatching service
/// </summary>
public class RecommendedAgentInfo
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public double AffinityScore { get; set; }
    public int CurrentWorkload { get; set; }
    public int MaxCapacity { get; set; }
    
    /// <summary>
    /// Workload percentage (0-100)
    /// </summary>
    public int WorkloadPercentage => MaxCapacity > 0 
        ? (int)((double)CurrentWorkload / MaxCapacity * 100) 
        : 0;
    
    /// <summary>
    /// Human-readable affinity explanation
    /// </summary>
    public string AffinityLabel => AffinityScore switch
    {
        >= 4.5 => "Excellent Match",
        >= 4.0 => "Good Match",
        >= 3.0 => "Fair Match",
        _ => "Suggested"
    };

}
