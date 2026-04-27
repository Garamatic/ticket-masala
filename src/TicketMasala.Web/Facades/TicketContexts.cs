using Microsoft.AspNetCore.Mvc.Rendering;
using TicketMasala.Domain.Configuration;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Facades;

public class TicketDetailContext
{
    public string DomainId { get; set; } = string.Empty;
    public EntityLabels EntityLabels { get; set; } = new();
    public List<CustomFieldDefinition> CustomFields { get; set; } = new();
    public string? WorkItemTypeCode { get; set; }
    public Dictionary<string, object> CustomFieldValues { get; set; } = new();
}

public class TicketCreateContext
{
    public string DomainId { get; set; } = string.Empty;
    public EntityLabels EntityLabels { get; set; } = new();
    public List<CustomFieldDefinition> CustomFields { get; set; } = new();
    public List<WorkItemTypeDefinition> WorkItemTypes { get; set; } = new();

    public IEnumerable<SelectListItem>? Employees { get; set; }
    public IEnumerable<SelectListItem>? Projects { get; set; }
    public IEnumerable<SelectListItem>? Customers { get; set; }

    public string? PreselectedCustomerId { get; set; }
    public Guid? PreselectedProjectId { get; set; }
    public bool IsCustomer { get; set; }
}

public class TicketEditContext
{
    public EditTicketViewModel ViewModel { get; set; } = new();

    public string DomainId { get; set; } = string.Empty;
    public EntityLabels EntityLabels { get; set; } = new();
    public List<CustomFieldDefinition> CustomFields { get; set; } = new();
    public string? WorkItemTypeCode { get; set; }
    public Dictionary<string, object> CustomFieldValues { get; set; } = new();
    public IEnumerable<SelectListItem>? ValidStatuses { get; set; }
}
