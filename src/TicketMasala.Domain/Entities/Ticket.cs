using System.ComponentModel.DataAnnotations.Schema;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Events;
using TicketMasala.Domain.Exceptions;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a work item (ticket) in the system.
/// This is the core domain entity for tracking and managing work.
/// </summary>
public class Ticket : BaseModel, IAggregateRoot, IHasDomainEvents
{
    // Note: Domain events are now managed by BaseModel.
    // Use RaiseDomainEvent() and DomainEventsNew from base class.
    public TicketType? TicketType { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime? CompletionTarget { get; set; }
    public DateTime? CompletionDate { get; set; }

    // GERDA AI fields
    public int EstimatedEffortPoints { get; set; } = 0;

    /// <summary>
    /// Runtime priority score calculated by GERDA AI (0-100 scale).
    /// This is the primary property for business logic and display.
    /// For database queries, use ComputedPriority which is generated from CustomFieldsJson.
    /// </summary>
    public double PriorityScore { get; set; } = 0.0;

    [StringLength(1000)]
    public string? GerdaTags { get; set; } // Comma-separated: "AI-Dispatched,Spam-Cluster"

    // GERDA Dispatch fields
    public string? RecommendedProjectName { get; set; }
    public string? CurrentProjectName { get; set; }

    // --- RIGID COLUMNS (Indexed, Relational) ---
    public string DomainId { get; set; } = "IT"; // e.g., "IT", "LEGAL"

    /// <summary>
    /// Simplified lifecycle status for triage: "New", "Triaged", "Done".
    /// Use this for basic status filtering. For workflow state, use TicketStatus.
    /// </summary>
    public string Status { get; set; } = "New";

    /// <summary>
    /// Workflow state for ticket processing (Pending, InProgress, Completed, etc.).
    /// This is the primary status for business logic and GERDA workflow.
    /// </summary>
    public Status TicketStatus { get; set; } = Common.Status.Pending;

    public string Title { get; set; } = string.Empty;

    // Used for Duplicate Detection (SHA256)
    [MaxLength(64)]
    public string? ContentHash { get; set; }

    // Link to the Config Version active when this ticket was created
    [MaxLength(50)]
    public string? ConfigVersionId { get; set; }

    // --- FLEXIBLE STORAGE (The "Masala" Model) ---
    [Column(TypeName = "TEXT")]
    public string CustomFieldsJson { get; set; } = "{}";

    // --- GENERATED COLUMNS (The Performance Secret) ---
    // These properties do not exist in C# memory as settable values.
    // They are projected by the database from the JSON blob.
    public double? ComputedPriority { get; private set; } // Indexable Priority
    public string? ComputedCategory { get; private set; } // Indexable Category

    // ═══════════════════════════════════════════
    // DOMAIN EXTENSIBILITY FIELDS
    // ═══════════════════════════════════════════

    /// <summary>
    /// The domain this ticket belongs to (e.g., "IT", "Gardening", "TaxLaw").
    /// Defaults to "IT" for backward compatibility.
    /// </summary>
    [StringLength(50)]
    public string? WorkItemTypeCode { get; set; }

    /// <summary>
    /// JSON blob storing domain-specific custom field values.
    /// Schema is validated against the domain configuration.
    /// </summary>
    [Column(TypeName = "TEXT")] // For SQLite compatibility; use nvarchar(max) for SQL Server
    public string? DomainCustomFieldsJson { get; set; }

    // Navigation properties (configured via EF Core in Web layer)
    public Guid? ParentTicketGuid { get; set; }
    public string? CustomerId { get; set; }
    public Guid? ProjectGuid { get; set; }
    public string? ResponsibleId { get; set; }
    public List<string> WatcherIds { get; set; } = new List<string>();

    public Guid? SolvedByArticleId { get; set; }

    // AI-generated ticket summary
    [StringLength(2000)]
    public string? AiSummary { get; set; }

    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.None;

    // Navigation properties
    public virtual Project? Project { get; set; }
    public virtual ApplicationUser? Customer { get; set; }
    public virtual Employee? Responsible { get; set; }
    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public virtual ICollection<Ticket> SubTickets { get; set; } = new List<Ticket>();
    public virtual Ticket? ParentTicket { get; set; }

    /// <summary>
    /// Ensures Status and TicketStatus remain synchronized when TicketStatus changes.
    /// Call this after modifying TicketStatus to update the simplified Status field.
    /// </summary>
    public void SyncStatus()
    {
        Status = TicketStatus switch
        {
            Common.Status.Pending => "New",
            Common.Status.Assigned or Common.Status.InProgress => "Triaged",
            Common.Status.Completed => "Done",
            _ => "New"
        };
    }

    // Backwards-compatibility: ensure members have safe defaults
    public Ticket()
    {
        Description = string.Empty;
        Title = string.Empty;
        DomainId = "IT";
        CustomFieldsJson = "{}";
        TicketStatus = Common.Status.Pending;
        SyncStatus(); // Ensure Status is synchronized on creation
    }

    // ═══════════════════════════════════════════
    // DOMAIN EVENT RAISES (Phase 1: Non-breaking)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Raises a domain event when this ticket is created.
    /// Call this immediately after creating a new ticket.
    /// </summary>
    public void RaiseCreatedEvent(string customerId)
    {
        RaiseDomainEvent(new TicketCreatedEvent(Guid, customerId, DomainId));
    }

    /// <summary>
    /// Raises a domain event when this ticket is assigned.
    /// </summary>
    public void RaiseAssignedEvent(string newResponsibleId, string? oldResponsibleId, string assignedByUserId)
    {
        RaiseDomainEvent(new TicketAssignedEvent(Guid, newResponsibleId, oldResponsibleId, assignedByUserId));
    }

    /// <summary>
    /// Raises a domain event when this ticket's status changes.
    /// </summary>
    public void RaiseStatusChangedEvent(Status oldStatus, Status newStatus, string changedByUserId)
    {
        RaiseDomainEvent(new TicketStatusChangedEvent(Guid, oldStatus, newStatus, changedByUserId));
    }

    /// <summary>
    /// Raises a domain event when this ticket is updated.
    /// </summary>
    public void RaiseUpdatedEvent(string propertyName, string updatedByUserId)
    {
        RaiseDomainEvent(new TicketUpdatedEvent(Guid, propertyName, updatedByUserId));
    }
}

