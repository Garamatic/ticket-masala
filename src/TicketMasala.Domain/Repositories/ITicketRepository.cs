using System.Text.Json.Serialization;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Repositories;

/// <summary>
/// Repository interface for Ticket entity operations.
/// Implements Repository pattern to abstract data access from business logic.
/// </summary>
public interface ITicketRepository
{
    // Read operations
    Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<IReadOnlyList<Ticket>> GetAllAsync(Guid? departmentId = null);
    Task<IReadOnlyList<Ticket>> GetUnassignedAsync(Guid? departmentId = null);
    Task<IReadOnlyList<Ticket>> GetByStatusAsync(Status status, Guid? departmentId = null);
    Task<IReadOnlyList<Ticket>> GetByCustomerIdAsync(string customerId);
    Task<IReadOnlyList<Ticket>> GetByResponsibleIdAsync(string responsibleId);
    Task<IReadOnlyList<Ticket>> GetByProjectGuidAsync(Guid projectGuid);
    Task<IReadOnlyList<Ticket>> GetRecentAsync(int timeWindowMinutes, Guid? departmentId = null);
    Task<IReadOnlyList<Ticket>> GetPendingOrAssignedAsync(Guid? departmentId = null);
    Task<(IReadOnlyList<TicketSearchResultDto> Results, int TotalItems)> SearchAsync(TicketSearchQuery query);

    // Aggregate queries
    Task<IReadOnlyList<Ticket>> GetActiveTicketsAsync();
    Task<IReadOnlyList<Ticket>> GetCompletedTicketsAsync();
    Task<int> CountAsync();

    // Write operations
    Task<Ticket> AddAsync(Ticket ticket);
    Task UpdateAsync(Ticket ticket);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);

    // Related data
    Task<IReadOnlyList<Document>> GetDocumentsForTicketAsync(Guid ticketId);
    Task<IReadOnlyList<TicketComment>> GetCommentsForTicketAsync(Guid ticketId);
    Task<IReadOnlyList<QualityReview>> GetQualityReviewsForTicketAsync(Guid ticketId);
}

/// <summary>
/// Data Transfer Object for ticket search results.
/// </summary>
public class TicketSearchResultDto
{
    public Guid Guid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Status TicketStatus { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? CompletionTarget { get; set; }
    public string? CustomerName { get; set; }
    public string? ResponsibleName { get; set; }
    public string? ProjectName { get; set; }
    public Guid? ProjectGuid { get; set; }
    public string? GerdaTags { get; set; }

    // Computed properties for display (calculated client-side from CompletionTarget)
    public bool IsOverdue => CompletionTarget.HasValue && CompletionTarget.Value < DateTime.UtcNow;
    public bool IsDueSoon => CompletionTarget.HasValue && !IsOverdue && (CompletionTarget.Value - DateTime.UtcNow).TotalHours < 24;
    public double DaysUntilDue => CompletionTarget.HasValue ? (CompletionTarget.Value - DateTime.UtcNow).TotalDays : 0;

    // Navigation helpers (flattened for view compatibility)
    [JsonIgnore]
    public UserSummary? Customer => !string.IsNullOrEmpty(CustomerName) ? new UserSummary { Name = CustomerName } : null;

    [JsonIgnore]
    public UserSummary? Responsible => !string.IsNullOrEmpty(ResponsibleName) ? new UserSummary { Name = ResponsibleName } : null;
}

/// <summary>
/// User summary for display in search results.
/// </summary>
public class UserSummary
{
    public string Name { get; set; } = string.Empty;
    public string FirstName => Name.Split(' ')[0];
    public string LastName => Name.Contains(' ') ? Name.Substring(Name.IndexOf(' ') + 1) : string.Empty;
}

/// <summary>
/// Query object to encapsulate criteria for searching tickets.
/// </summary>
public class TicketSearchQuery
{
    public string? SearchTerm { get; set; }
    public Status? Status { get; set; }
    public TicketType? TicketType { get; set; }
    public string? ResponsibleId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? CustomerId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public Guid? DepartmentId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
