using System.ComponentModel.DataAnnotations;
using TicketMasala.Domain.Common;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a department within the organization.
/// Departments group projects and employees into functional units (e.g., Finance, HR).
/// </summary>
public class Department : BaseModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty; // e.g., "FIS", "HR"

    public List<Project> Projects { get; set; } = [];
}
