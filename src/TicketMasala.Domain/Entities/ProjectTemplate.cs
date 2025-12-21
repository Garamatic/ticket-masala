using System.ComponentModel.DataAnnotations;
using TicketMasala.Domain.Common;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a project template.
/// </summary>
public class ProjectTemplate : BaseModel
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [Required]
    [StringLength(2000)]
    public required string Description { get; set; }

    public List<TemplateTicket> Tickets { get; set; } = new();
}

