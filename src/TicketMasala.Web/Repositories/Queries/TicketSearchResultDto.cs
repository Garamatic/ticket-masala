using TicketMasala.Domain.Common;

namespace TicketMasala.Web.Repositories.Queries;

/// <summary>
/// Data Transfer Object for ticket search results.
/// Decouples the repository from full Domain entities for search performance and architectural purity.
/// </summary>
public class TicketSearchResultDto
{
    public Guid Guid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Status TicketStatus { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? CompletionTarget { get; set; }

    // Relationships (Flattened for display)
    public string? CustomerName { get; set; }
    public string? ResponsibleName { get; set; }

    // Simple shells for view compatibility
    public UserSummary? Customer => !string.IsNullOrEmpty(CustomerName) ? new UserSummary { Name = CustomerName } : null;
    public UserSummary? Responsible => !string.IsNullOrEmpty(ResponsibleName) ? new UserSummary { Name = ResponsibleName } : null;

    public string? ProjectName { get; set; }
    public Guid? ProjectGuid { get; set; }

    public string? GerdaTags { get; set; }
}

public class UserSummary
{
    public string Name { get; set; } = string.Empty;
    public string FirstName => Name.Split(' ')[0];
    public string LastName => Name.Contains(' ') ? Name.Substring(Name.IndexOf(' ') + 1) : string.Empty;
}
