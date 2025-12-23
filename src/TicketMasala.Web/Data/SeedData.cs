using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;

namespace TicketMasala.Web.Data;


public class SeedConfig
{
    public List<SeedUser> Admins { get; set; } = new();
    public List<SeedUser> Employees { get; set; } = new();
    public List<SeedUser> Customers { get; set; } = new();
    public List<SeedWorkContainer> WorkContainers { get; set; } = new();
    public List<SeedWorkItem> UnassignedWorkItems { get; set; } = new();
    public List<SeedKBArticle> KnowledgeBaseArticles { get; set; } = new();
}

public class SeedUser
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Team { get; set; }
    public EmployeeType? Level { get; set; }
    public string? Code { get; set; }

    // GERDA Fields
    public string? Language { get; set; }
    public string? Specializations { get; set; }
    public int? MaxCapacityPoints { get; set; }
    public string? Region { get; set; }
}

public class SeedWorkContainer
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Status Status { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string? ProjectManagerEmail { get; set; }
    public int CompletionTargetMonths { get; set; }
    public int? CompletedDaysAgo { get; set; }
    public List<SeedWorkItem> WorkItems { get; set; } = new();
}

public class SeedWorkItem
{
    public string Description { get; set; } = string.Empty;
    public Status Status { get; set; }
    public TicketType Type { get; set; } = TicketType.Subtask;
    public string? ResponsibleEmail { get; set; }
    public int CompletionTargetDays { get; set; }
    public int? CompletionDaysAgo { get; set; }
    public List<SeedComment> Comments { get; set; } = new();
    public double? EstimatedEffortPoints { get; set; }
    public double? PriorityScore { get; set; }
    public string? GerdaTags { get; set; }
    public string? CustomerEmail { get; set; } // For unassigned items
}

public class SeedComment
{
    public string Body { get; set; } = string.Empty;
    public string? AuthorEmail { get; set; } // Use email to look up ID
    public int CreatedDaysAgo { get; set; }
}

public class SeedKBArticle
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string? AuthorEmail { get; set; }
    public int CreatedDaysAgo { get; set; }
}
