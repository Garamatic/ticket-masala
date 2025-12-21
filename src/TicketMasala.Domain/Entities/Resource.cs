using TicketMasala.Domain.Common;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a resource associated with a project.
/// </summary>
public class Resource : BaseModel
{
    // Could be path or url
    public required string Location { get; set; }
    // TODO: implement resource permissions
}

