using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.Tickets;

public class CreateTicketViewModel
{
    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    public string? CustomerId { get; set; }

    public string? ResponsibleId { get; set; }

    public Guid? ProjectGuid { get; set; }

    public DateTime? CompletionTarget { get; set; }

    public string? DomainId { get; set; }

    public string? WorkItemTypeCode { get; set; }
}
