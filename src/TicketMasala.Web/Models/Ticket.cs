using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using TicketMasala.Web.Utilities;

namespace TicketMasala.Web.Models;
public class Ticket : BaseModel
{
    public required Status TicketStatus { get; set; } = Models.Status.Pending;
    public TicketType? TicketType { get; set; }
    
    [Required(ErrorMessage = "Description is required")]
    [NoHtml(ErrorMessage = "Description cannot contain HTML")]
    [SafeStringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public required string Description { get; set; }
    
    public DateTime? CompletionTarget { get; set; }
    public DateTime? CompletionDate { get; set; }

    // GERDA AI fields
    public int EstimatedEffortPoints { get; set; } = 0;
    public double PriorityScore { get; set; } = 0.0;
    
    [SafeStringLength(1000, ErrorMessage = "Tags cannot exceed 1000 characters")]
    public string? GerdaTags { get; set; } // Comma-separated: "AI-Dispatched,Spam-Cluster"

    // GERDA Dispatch fields
    public string? RecommendedProjectName { get; set; }
    public string? CurrentProjectName { get; set; }

    // --- RIGID COLUMNS (Indexed, Relational) ---
    [Required]
    [MaxLength(50)]
    public required string DomainId { get; set; } // e.g., "IT", "LEGAL"

    [Required]
    [MaxLength(20)]
    public required string Status { get; set; } = "New"; // "New", "Triaged", "Done"

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    // Used for Duplicate Detection (SHA256)
    [MaxLength(64)]
    public string? ContentHash { get; set; }

    // Link to the Config Version active when this ticket was created
    [MaxLength(50)]
    public string? ConfigVersionId { get; set; }

    // --- FLEXIBLE STORAGE (The "Masala" Model) ---
    [Column(TypeName = "TEXT")]
    public required string CustomFieldsJson { get; set; } = "{}";

    // --- GENERATED COLUMNS (The Performance Secret) ---
    // These properties do not exist in C# memory as settable values.
    // They are projected by SQLite from the JSON blob.
    public double? ComputedPriority { get; private set; } // Indexable Priority
    public string? ComputedCategory { get; private set; } // Indexable Category



    // ═══════════════════════════════════════════
    // DOMAIN EXTENSIBILITY FIELDS
    // ═══════════════════════════════════════════
    
    /// <summary>
    /// The domain this ticket belongs to (e.g., "IT", "Gardening", "TaxLaw").
    /// Defaults to "IT" for backward compatibility.
    /// </summary>
    [SafeStringLength(50)]
    public string? WorkItemTypeCode { get; set; }

    /// <summary>
    /// JSON blob storing domain-specific custom field values.
    /// Schema is validated against the domain configuration.
    /// </summary>
    [Column(TypeName = "TEXT")] // For SQLite compatibility; use nvarchar(max) for SQL Server
    public string? DomainCustomFieldsJson { get; set; }

    public Ticket? ParentTicket { get; set; }
    public Guid? ParentTicketGuid { get; set; }
    public List<Ticket> SubTickets { get; set; } = new List<Ticket>();
    // Backwards-compatible customer nav/id (previously present in the model)
    // Use `ApplicationUser` here so callers that supply `ApplicationUser` instances
    // (e.g., from `_context.Users`) can assign directly without conversion errors.
    public ApplicationUser? Customer { get; set; }
    public string? CustomerId { get; set; }

    // Backwards-compatible project linkage
    public Guid? ProjectGuid { get; set; }
    public Project? Project { get; set; }

    // Quality reviews were removed earlier; reintroduce an empty collection for compatibility
    public List<QualityReview> QualityReviews { get; set; } = new List<QualityReview>();
    public ApplicationUser? Responsible { get; set; }
    public string? ResponsibleId { get; set; }
    public List<ApplicationUser> Watchers { get; set; } = new List<ApplicationUser>();
    public List<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public List<Document> Attachments { get; set; } = new List<Document>();

    public Guid? SolvedByArticleId { get; set; }
    [ForeignKey("SolvedByArticleId")]
    public KnowledgeBaseArticle? SolvedByArticle { get; set; }

    // AI-generated ticket summary
    [SafeStringLength(2000, ErrorMessage = "AI summary cannot exceed 2000 characters")]
    public string? AiSummary { get; set; }

    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.None;
    // QualityReviews deleted

    // Backwards-compatibility: ensure required members have safe defaults so
    // object initializers used across the codebase don't fail (CS9035).
    [SetsRequiredMembers]
    public Ticket()
    {
        Description = string.Empty;
        Title = string.Empty;
        DomainId = "IT";
        CustomFieldsJson = "{}";
        TicketStatus = Models.Status.Pending;
    }
}
