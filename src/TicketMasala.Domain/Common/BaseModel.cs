using System.ComponentModel.DataAnnotations;
using TicketMasala.Domain.Events;

namespace TicketMasala.Domain.Common;

/// <summary>
/// Base class for all domain entities providing common properties.
/// Now implements IHasDomainEvents for rich domain model support.
/// </summary>
public abstract class BaseModel : IHasDomainEvents
{
    [Key]
    public Guid Guid { get; set; } = Guid.NewGuid();
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }
    public DateTime? ValidUntil { get; set; }
    public Guid? CreatorGuid { get; set; }

    // Domain events collection (legacy - preserved for backward compatibility)
    private readonly List<DomainEvent> _domainEvents = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // New: IDomainEvent support for rich domain model
    private readonly List<IDomainEvent> _domainEventsNew = new();

    /// <summary>
    /// Read-only collection of domain events implementing the new IDomainEvent interface.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyCollection<IDomainEvent> DomainEventsNew => _domainEventsNew.AsReadOnly();

    // Explicit interface implementation for IHasDomainEvents
    IReadOnlyCollection<IDomainEvent> IHasDomainEvents.DomainEvents => _domainEventsNew.AsReadOnly();

    /// <summary>
    /// Adds a legacy domain event to the collection.
    /// </summary>
    protected void AddDomainEvent(DomainEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    /// <summary>
    /// Adds a domain event using the new IDomainEvent interface.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent eventItem)
    {
        _domainEventsNew.Add(eventItem);
    }

    /// <summary>
    /// Clears all legacy domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Clears all domain events implementing IDomainEvent.
    /// </summary>
    void IHasDomainEvents.ClearDomainEvents()
    {
        _domainEventsNew.Clear();
    }
}

/// <summary>
/// Marker class for domain events. Inherit from this to create specific event types.
/// Now implements IDomainEvent for compatibility with the new rich domain model.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
