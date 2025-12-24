using TicketMasala.Domain.Common;

namespace TicketMasala.Web.Repositories.Queries;

/// <summary>
/// Query object to encapsulate criteria for searching tickets.
/// Pattern: Query Object - decouples search logic from ViewModels and Repository.
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

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
