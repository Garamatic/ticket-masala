using System.ComponentModel.DataAnnotations.Schema;
using TicketMasala.Domain.Common;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a work item (ticket) in the system.
/// This is the core domain entity for tracking and managing work.
/// </summary>
public class Ticket : BaseModel
{
    public Status TicketStatus { get; set; } = Common.Status.Pending;
    public TicketType? TicketType { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime? CompletionTarget { get; set; }
    public DateTime? CompletionDate { get; set; }

    // GERDA AI fields
    public int EstimatedEffortPoints { get; set; } = 0;
    public double PriorityScore { get; set; } = 0.0;

    [StringLength(1000)]
    public string? GerdaTags { get; set; } // Comma-separated: "AI-Dispatched,Spam-Cluster"

    // GERDA Dispatch fields
    public string? RecommendedProjectName { get; set; }
    public string? CurrentProjectName { get; set; }

    // --- RIGID COLUMNS (Indexed, Relational) ---
    public string DomainId { get; set; } = "IT"; // e.g., "IT", "LEGAL"

    public string Status { get; set; } = "New"; // "New", "Triaged", "Done"

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

    // Backwards-compatibility: ensure members have safe defaults
    public Ticket()
    {
        Description = string.Empty;
        Title = string.Empty;
        DomainId = "IT";
        CustomFieldsJson = "{}";
        TicketStatus = Common.Status.Pending;
    }
}

