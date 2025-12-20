using TicketMasala.Web.Models;

namespace TicketMasala.Web.ViewModels.Tickets;

public class TicketSearchViewModel
{
    public string? SearchTerm { get; set; }
    public Status? Status { get; set; }
    public TicketType? TicketType { get; set; }
    public string? ResponsibleId { get; set; }
    // Alias for ResponsibleId to match Controller usage
    public string? AssignedToId
    {
        get => ResponsibleId;
        set => ResponsibleId = value;
    }

    public Guid? ProjectId { get; set; }
    public string? CustomerId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool ShowOnlyMyTickets { get; set; }

    public bool IsOverdue { get; set; }
    public bool IsDueSoon { get; set; }

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    // Dropdowns
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Customers { get; set; } = new();
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Employees { get; set; } = new();
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Projects { get; set; } = new();

    // Results
    public List<Ticket> Results { get; set; } = new();
}
