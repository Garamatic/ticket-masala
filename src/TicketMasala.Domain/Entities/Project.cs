using System.ComponentModel.DataAnnotations;
using TicketMasala.Domain.Common;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a work container (project) that groups related tickets.
/// </summary>
public class Project : BaseModel
{
    public Status Status { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // User references (configured via EF Core in Web layer)
    public string? ProjectManagerId { get; set; }
    public string? CustomerId { get; set; }
    public List<string> CustomerIds { get; set; } = new List<string>();

    public DateTime? CompletionTarget { get; set; }
    public DateTime? CompletionDate { get; set; }

    // Department reference (optional, for organizational hierarchy)
    public Guid? DepartmentId { get; set; }

    public string? ProjectType { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    // AI-generated project roadmap
    [StringLength(10000)]
    public string? ProjectAiRoadmap { get; set; }

    // Navigation properties
    public virtual Employee? ProjectManager { get; set; }
    public virtual ICollection<Ticket> Tasks { get; set; } = new List<Ticket>();
    public virtual ApplicationUser? Customer { get; set; }
    public virtual ICollection<ApplicationUser> Customers { get; set; } = new List<ApplicationUser>(); // Stakeholders
    public virtual ICollection<Employee> Resources { get; set; } = new List<Employee>();
}

