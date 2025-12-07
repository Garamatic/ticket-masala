using System.ComponentModel.DataAnnotations;
using TicketMasala.Web.Utilities;

namespace TicketMasala.Web.Models;
public class ProjectTemplate : BaseModel
{
    [Required(ErrorMessage = "Template name is required")]
    [NoHtml]
    [SafeStringLength(200)]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [NoHtml]
    [SafeStringLength(2000)]
    public required string Description { get; set; }

    public List<TemplateTicket> Tickets { get; set; } = new();
}
