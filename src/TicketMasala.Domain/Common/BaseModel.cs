using System.ComponentModel.DataAnnotations;
using TicketMasala.Domain.Events;

namespace TicketMasala.Domain.Common;

/// <summary>
/// Base class for all domain entities providing common properties.
/// Implements IHasDomainEvents for rich domain model support.
/// </summary>
public abstract class BaseModel : IHasDomainEvents
{
    [Key]
    public Guid Guid { get; set; } = Guid.NewGuid();
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }
    public DateTime? ValidUntil { get; set; }
    public Guid? CreatorGuid { get; set; }

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Read-only collection of domain events that have been raised but not yet dispatched.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    IReadOnlyCollection<IDomainEvent> IHasDomainEvents.DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    /// <summary>
    /// Clears all pending domain events after they have been dispatched.
    /// </summary>
    void IHasDomainEvents.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
