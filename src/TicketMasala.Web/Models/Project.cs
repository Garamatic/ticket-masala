using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TicketMasala.Web.Utilities;

namespace TicketMasala.Web.Models;
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
    
    public ApplicationUser? Customer { get; set; }
    public string? CustomerId { get; set; }
    
    public ICollection<ApplicationUser> Customers { get; set; } = new List<ApplicationUser>();

    public DateTime? CompletionTarget { get; set; }
    public DateTime? CompletionDate { get; set; }

    public List<Ticket> Tasks { get; set; } = [];
    public List<Resource> Resources { get; set; } = [];

    // Department reference (optional, for organizational hierarchy)
    public Guid? DepartmentId { get; set; }

    // AI-generated project roadmap
    [SafeStringLength(10000, ErrorMessage = "AI roadmap cannot exceed 10000 characters")]
    public string? ProjectAiRoadmap { get; set; }
}
