using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using IT_Project2526.Utilities;

namespace IT_Project2526.Models
{
    public class Project : BaseModel
    {
        public Status Status { get; set; }
        
        [Required(ErrorMessage = "Project name is required")]
        [NoHtml(ErrorMessage = "Project name cannot contain HTML")]
        [SafeStringLength(200, ErrorMessage = "Project name cannot exceed 200 characters")]
        public required string Name { get; set; }
        
        [Required(ErrorMessage = "Project description is required")]
        [NoHtml(ErrorMessage = "Project description cannot contain HTML")]
        [SafeStringLength(5000, ErrorMessage = "Project description cannot exceed 5000 characters")]
        public required string Description { get; set; }
        
        public Employee? ProjectManager { get; set; }
        public string? ProjectManagerId { get; set; }
        
        public Customer? Customer { get; set; }
        public string? CustomerId { get; set; }

        public DateTime? CompletionTarget { get; set; }
        public DateTime? CompletionDate { get; set; }

        public List<Ticket> Tasks { get; set; } = [];
        public List<Resource> Resources { get; set; } = [];
    }
}