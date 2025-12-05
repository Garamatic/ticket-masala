using System.ComponentModel.DataAnnotations;
using IT_Project2526.Utilities;

namespace IT_Project2526.Models
{
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
}
