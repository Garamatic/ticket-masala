using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TicketMasala.Web.Utilities;

namespace TicketMasala.Web.Models;
public class TemplateTicket : BaseModel
{
    [Required(ErrorMessage = "Description is required")]
    [NoHtml]
    [SafeStringLength(1000)]
    public required string Description { get; set; }

    [Range(1, 100)]
    public int EstimatedEffortPoints { get; set; } = 5;

    public Priority Priority { get; set; } = Priority.Medium;
    
    public TicketType TicketType { get; set; } = TicketType.Task;

    public Guid ProjectTemplateId { get; set; }
    
    [ForeignKey("ProjectTemplateId")]
    public ProjectTemplate? ProjectTemplate { get; set; }
}
