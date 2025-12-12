namespace TicketMasala.Web.ViewModels.Api;

/// <summary>
/// Request model for creating a WorkItem (Universal Entity Model).
/// Valid DomainId values are sourced from masala_domains.yaml configuration.
/// </summary>
public class CreateWorkItemRequest
{
    /// <summary>
    /// Title/subject of the work item
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Detailed description of the work item
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The domain identifier for this work item.
    /// The valid set of domain IDs is entirely governed by the structure keys in masala_domains.yaml.
    /// Any change requires a configuration update and delegate recompilation by the RuleCompilerService.
    /// Examples: "IT", "Gardening"
    /// </summary>
    public string DomainId { get; set; } = "IT";

    /// <summary>
    /// Optional customer ID to associate with the work item
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Optional assignee ID. If null, GERDA AI may auto-assign.
    /// </summary>
    public string? AssigneeId { get; set; }

    /// <summary>
    /// Optional work container (project) to associate with
    /// </summary>
    public Guid? WorkContainerId { get; set; }

    /// <summary>
    /// Target completion date
    /// </summary>
    public DateTime? CompletionTarget { get; set; }

    /// <summary>
    /// Domain-specific custom fields. Schema validated against masala_domains.yaml.
    /// </summary>
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>
/// Response model for WorkItem operations (Universal Entity Model).
/// </summary>
public class WorkItemResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DomainId { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletionTarget { get; set; }
    public DateTime? CompletedAt { get; set; }

    // GERDA AI fields
    public int? EstimatedEffortPoints { get; set; }
    public double? PriorityScore { get; set; }
    public string? RecommendedAssignee { get; set; }

    // Relationships
    public string? CustomerName { get; set; }
    public string? AssigneeName { get; set; }
    public Guid? WorkContainerId { get; set; }
    public string? WorkContainerName { get; set; }
}

/// <summary>
/// List response wrapper for paginated WorkItem results
/// </summary>
public class WorkItemListResponse
{
    public List<WorkItemResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
