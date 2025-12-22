using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectListItem

namespace TicketMasala.Web.ViewModels.Portal;

public class InternalCreateTicketViewModel
{
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    public string? CustomerId { get; set; }

    public string? ResponsibleId { get; set; }

    public Guid? ProjectGuid { get; set; }

    public DateTime? CompletionTarget { get; set; }

    public string? DomainId { get; set; }

    public string? WorkItemTypeCode { get; set; }

    // For the dropdown list of projects
    public IEnumerable<SelectListItem>? Projects { get; set; }
}
