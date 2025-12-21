using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Domain.Common;

/// <summary>
/// Base class for all domain entities providing common properties.
/// </summary>
public abstract class BaseModel
{
    [Key]
    public Guid Guid { get; set; } = Guid.NewGuid();
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntil { get; set; }
    public Guid? CreatorGuid { get; set; }
}

