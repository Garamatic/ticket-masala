using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using TicketMasala.Web.Utilities;

namespace TicketMasala.Web.Models;
    public class Ticket : BaseModel
    {
        public required Status TicketStatus { get; set; } = Status.Pending;
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
        public string CustomFieldsJson { get; set; } = "{}";

        // --- GENERATED COLUMNS (Read-Only Performance Optimizations) ---
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public double? ComputedPriority { get; private set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? ComputedCategory { get; private set; }

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
        public required string CustomFieldsJson { get; set; } = "{}";

        public Ticket? ParentTicket { get; set; }
        public Guid? ParentTicketGuid { get; set; }
        public List<Ticket> SubTickets { get; set; } = [];
        public ApplicationUser? Responsible { get; set; }
        public string? ResponsibleId { get; set; }
        public List<ApplicationUser> Watchers { get; set; } = [];
        public List<TicketComment> Comments { get; set; } = new();
        public List<Document> Attachments { get; set; } = [];

        public Guid? SolvedByArticleId { get; set; }
        [ForeignKey("SolvedByArticleId")]
        public KnowledgeBaseArticle? SolvedByArticle { get; set; }

        public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.None;
        // QualityReviews deleted
}
