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

    // Domain events for significant state changes
    private readonly List<DomainEvent> _domainEvents = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// Marker class for domain events. Inherit from this to create specific event types.
/// </summary>
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

