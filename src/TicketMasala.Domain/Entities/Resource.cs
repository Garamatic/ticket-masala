using TicketMasala.Domain.Common;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a resource associated with a project.
/// </summary>
public class Resource : BaseModel
{
    // Could be path or url
    public string Location { get; set; } = string.Empty;
    // TODO: implement resource permissions
}

