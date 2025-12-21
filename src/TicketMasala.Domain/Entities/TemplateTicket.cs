using System.ComponentModel.DataAnnotations;
using TicketMasala.Domain.Common;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a ticket template within a project template.
/// </summary>
public class TemplateTicket : BaseModel
{
    public string Description { get; set; } = string.Empty;

    [Range(1, 100)]
    public int EstimatedEffortPoints { get; set; } = 5;

    public Priority Priority { get; set; } = Priority.Medium;

    public TicketType TicketType { get; set; } = TicketType.Task;

    public Guid ProjectTemplateId { get; set; }
}

