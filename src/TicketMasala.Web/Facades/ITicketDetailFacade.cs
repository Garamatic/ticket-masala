using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Domain.Configuration;

namespace TicketMasala.Web.Facades;

public interface ITicketDetailFacade
{
    Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketId, string? userId, bool isCustomer);
    
    // Also include the ViewBags logic if possible, or return a composite object.
    // The controller sets ViewBags: DomainId, EntityLabels, CustomFields, WorkItemTypeCode, CustomFieldValues.
    // It's cleaner to return a "TicketDetailResult" that contains ViewModel + Context Data.
    Task<TicketDetailContext> GetTicketDetailContextAsync(TicketDetailsViewModel viewModel);
}

public class TicketDetailContext
{
    public string DomainId { get; set; } = string.Empty;
    public EntityLabels EntityLabels { get; set; } = new();
    public List<CustomFieldDefinition> CustomFields { get; set; } = new();
    public string? WorkItemTypeCode { get; set; }
    public Dictionary<string, object> CustomFieldValues { get; set; } = new();
}
