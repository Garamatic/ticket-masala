using System.ComponentModel.DataAnnotations.Schema;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Events;
using TicketMasala.Domain.Exceptions;

namespace TicketMasala.Domain.Entities;

/// <summary>
/// Represents a work item (ticket) in the system.
/// This is the core domain entity for tracking and managing work.
/// Now implements Rich Domain Model pattern with encapsulated behavior.
/// </summary>
public class Ticket : BaseModel, IAggregateRoot, IHasDomainEvents
{
    // ═════════════════════════════════════════════════════════════════
    // ENCAPSULATED PROPERTIES (Phase 2: internal set for migration compatibility)
    // ═════════════════════════════════════════════════════════════════
    // Note: Using 'internal set' allows the Web layer to set properties during
    // the migration period. Goal is to gradually move to rich methods.

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

    // ═════════════════════════════════════════════════════════════════
    // DOMAIN EXTENSIBILITY FIELDS
    // ═════════════════════════════════════════════════════════════════

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

    // ═════════════════════════════════════════════════════════════════
    // NAVIGATION PROPERTIES (Managed by EF Core, not directly set)
    // ═════════════════════════════════════════════════════════════════

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

    // ═════════════════════════════════════════════════════════════════
    // CONSTRUCTOR (EF Core requires parameterless constructor)
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// Do not use directly - use Ticket.Create() factory method instead.
    /// </summary>
    public Ticket()
    {
        Description = string.Empty;
        Title = string.Empty;
        DomainId = "IT";
        CustomFieldsJson = "{}";
        TicketStatus = Common.Status.Pending;
        // Note: WatcherIds is initialized by property initializer
        SyncStatus(); // Ensure Status is synchronized on creation
    }

    // ═════════════════════════════════════════════════════════════════
    // FACTORY METHODS (Phase 2: Rich creation with validation)
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new ticket with validation and business rules.
    /// This is the primary way to create tickets in the domain.
    /// </summary>
    /// <param name="description">The ticket description (required, max 5000 chars)</param>
    /// <param name="title">The ticket title (required, max 200 chars)</param>
    /// <param name="customerId">The ID of the customer creating the ticket</param>
    /// <param name="domainId">The domain ID (defaults to "IT")</param>
    /// <param name="projectGuid">Optional project association</param>
    /// <param name="workItemTypeCode">Optional work item type code</param>
    /// <returns>A new ticket with domain event raised</returns>
    /// <exception cref="DomainException">Thrown when validation fails</exception>
    public static Ticket Create(
        string description,
        string title,
        string? customerId,
        string? domainId = null,
        Guid? projectGuid = null,
        string? workItemTypeCode = null)
    {
        // Validate description
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Ticket description is required");

        if (description.Length > 5000)
            throw new DomainException("Description cannot exceed 5000 characters");

        // Validate title
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Ticket title is required");

        if (title.Length > 200)
            throw new DomainException("Title cannot exceed 200 characters");

        var ticket = new Ticket
        {
            Guid = Guid.NewGuid(),
            Description = description.Trim(),
            Title = title.Trim(),
            CustomerId = customerId,
            DomainId = domainId?.Trim() ?? "IT",
            ProjectGuid = projectGuid,
            WorkItemTypeCode = workItemTypeCode?.Trim(),
            TicketStatus = Common.Status.Pending,
            CreationDate = DateTime.UtcNow,
            PriorityScore = 0.0,
            CustomFieldsJson = "{}"
        };

        ticket.SyncStatus();

        // Raise domain event
        if (!string.IsNullOrEmpty(customerId))
        {
            ticket.RaiseDomainEvent(new TicketCreatedEvent(ticket.Guid, customerId, ticket.DomainId));
        }

        return ticket;
    }

    /// <summary>
    /// Creates a ticket from portal submission with minimal required fields.
    /// </summary>
    public static Ticket CreateFromPortal(
        string description,
        string? customerId,
        double? priorityScore = null,
        string? tags = null,
        DateTime? completionTarget = null)
    {
        // Auto-generate title from description
        var title = description.Length > 50
            ? description[..47] + "..."
            : description;

        var ticket = Create(description, title, customerId);

        if (priorityScore.HasValue)
            ticket.SetPriorityScore(priorityScore.Value);

        if (!string.IsNullOrEmpty(tags))
            ticket.SetGerdaTags(tags);

        if (completionTarget.HasValue)
            ticket.SetCompletionTarget(completionTarget.Value);

        return ticket;
    }

    // ═════════════════════════════════════════════════════════════════
    // DOMAIN BEHAVIOR METHODS (Phase 2: Encapsulated business logic)
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the ticket description with validation.
    /// </summary>
    /// <param name="newDescription">The new description</param>
    /// <param name="updatedByUserId">The user making the update</param>
    /// <exception cref="DomainException">Thrown when validation fails</exception>
    public void UpdateDescription(string newDescription, string updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            throw new DomainException("Description cannot be empty");

        if (newDescription.Length > 5000)
            throw new DomainException("Description cannot exceed 5000 characters");

        var oldDescription = Description;
        Description = newDescription.Trim();
        LastModified = DateTime.UtcNow;

        RaiseDomainEvent(new TicketUpdatedEvent(Guid, nameof(Description), updatedByUserId));
    }

    /// <summary>
    /// Updates the ticket title with validation.
    /// </summary>
    public void UpdateTitle(string newTitle, string updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new DomainException("Title cannot be empty");

        if (newTitle.Length > 200)
            throw new DomainException("Title cannot exceed 200 characters");

        Title = newTitle.Trim();
        LastModified = DateTime.UtcNow;

        RaiseDomainEvent(new TicketUpdatedEvent(Guid, nameof(Title), updatedByUserId));
    }

    /// <summary>
    /// Assigns the ticket to an employee.
    /// </summary>
    /// <param name="responsibleId">The employee ID to assign to</param>
    /// <param name="assignedByUserId">The user performing the assignment</param>
    /// <exception cref="DomainException">Thrown when assignment is invalid</exception>
    public void AssignTo(string responsibleId, string assignedByUserId)
    {
        if (string.IsNullOrWhiteSpace(responsibleId))
            throw new DomainException("Responsible ID is required for assignment");

        var oldResponsibleId = ResponsibleId;
        ResponsibleId = responsibleId;

        // Auto-transition to Assigned status if currently Pending
        if (TicketStatus == Common.Status.Pending)
        {
            var oldStatus = TicketStatus;
            TicketStatus = Common.Status.Assigned;
            SyncStatus();

            RaiseDomainEvent(new TicketStatusChangedEvent(Guid, oldStatus, Common.Status.Assigned, assignedByUserId));
        }

        RaiseDomainEvent(new TicketAssignedEvent(Guid, responsibleId, oldResponsibleId, assignedByUserId));
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Unassigns the ticket (sets it back to unassigned state).
    /// </summary>
    public void Unassign(string unassignedByUserId)
    {
        var oldResponsibleId = ResponsibleId;
        ResponsibleId = null;

        RaiseDomainEvent(new TicketAssignedEvent(Guid, "(unassigned)", oldResponsibleId, unassignedByUserId));
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the ticket to a new status with state machine validation.
    /// </summary>
    /// <param name="newStatus">The target status</param>
    /// <param name="changedByUserId">The user changing the status</param>
    /// <exception cref="DomainException">Thrown when transition is invalid</exception>
    public void TransitionTo(Status newStatus, string changedByUserId)
    {
        if (!IsValidTransition(TicketStatus, newStatus))
        {
            throw new DomainException(
                $"Cannot transition ticket from {TicketStatus} to {newStatus}. " +
                $"Valid transitions from {TicketStatus} are: {GetValidTransitions(TicketStatus)}");
        }

        var oldStatus = TicketStatus;
        TicketStatus = newStatus;
        SyncStatus();

        // Set completion date if transitioning to Completed
        if (newStatus == Common.Status.Completed && !CompletionDate.HasValue)
        {
            CompletionDate = DateTime.UtcNow;
        }

        // Clear completion date if reopening
        if (oldStatus == Common.Status.Completed && newStatus != Common.Status.Completed)
        {
            CompletionDate = null;
        }

        RaiseDomainEvent(new TicketStatusChangedEvent(Guid, oldStatus, newStatus, changedByUserId));
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the completion date directly (for seeding/migration scenarios).
    /// </summary>
    public void SetCompletionDate(DateTime? completionDate)
    {
        CompletionDate = completionDate;
    }

    /// <summary>
    /// Updates custom fields JSON with validation.
    /// </summary>
    public void UpdateCustomFields(string customFieldsJson, string updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(customFieldsJson))
            throw new DomainException("Custom fields JSON cannot be empty");

        CustomFieldsJson = customFieldsJson;
        LastModified = DateTime.UtcNow;

        RaiseDomainEvent(new TicketUpdatedEvent(Guid, nameof(CustomFieldsJson), updatedByUserId));
    }

    /// <summary>
    /// Adds a tag to the GerdaTags collection.
    /// </summary>
    public void AddGerdaTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        tag = tag.Trim();

        if (string.IsNullOrEmpty(GerdaTags))
        {
            GerdaTags = tag;
        }
        else if (!GerdaTags.Split(',').Select(t => t.Trim()).Contains(tag))
        {
            GerdaTags = $"{GerdaTags},{tag}";
        }
    }

    /// <summary>
    /// Sets the GERDA AI summary.
    /// </summary>
    public void SetAiSummary(string? summary)
    {
        if (!string.IsNullOrEmpty(summary) && summary.Length > 2000)
            throw new DomainException("AI summary cannot exceed 2000 characters");

        AiSummary = summary;
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the priority score (typically called by GERDA AI).
    /// </summary>
    public void SetPriorityScore(double score)
    {
        PriorityScore = Math.Clamp(score, 0.0, 100.0);
    }

    /// <summary>
    /// Sets the completion target date.
    /// </summary>
    public void SetCompletionTarget(DateTime target)
    {
        CompletionTarget = target;
    }

    /// <summary>
    /// Sets the content hash for duplicate detection.
    /// </summary>
    public void SetContentHash(string? hash)
    {
        ContentHash = hash;
    }

    /// <summary>
    /// Sets the config version ID.
    /// </summary>
    public void SetConfigVersionId(string? configVersionId)
    {
        ConfigVersionId = configVersionId;
    }

    /// <summary>
    /// Sets GERDA tags (replaces existing tags).
    /// </summary>
    public void SetGerdaTags(string? tags)
    {
        GerdaTags = tags;
    }

    /// <summary>
    /// Sets the estimated effort points (typically called by GERDA AI).
    /// </summary>
    public void SetEstimatedEffortPoints(int points)
    {
        EstimatedEffortPoints = Math.Max(0, points);
    }

    /// <summary>
    /// Sets the project association.
    /// </summary>
    public void SetProject(Guid? projectGuid)
    {
        ProjectGuid = projectGuid;
    }

    /// <summary>
    /// Sets the parent ticket for grouping.
    /// </summary>
    public void SetParentTicket(Guid? parentTicketGuid)
    {
        ParentTicketGuid = parentTicketGuid;
    }

    /// <summary>
    /// Sets the responsible employee (for direct assignment without full domain logic).
    /// Use AssignTo() for proper domain behavior with events.
    /// </summary>
    public void SetResponsibleId(string? responsibleId)
    {
        ResponsibleId = responsibleId;
    }

    /// <summary>
    /// Sets the responsible employee navigation property.
    /// </summary>
    public void SetResponsible(Employee? responsible)
    {
        Responsible = responsible;
        if (responsible != null)
        {
            ResponsibleId = responsible.Id;
        }
    }

    /// <summary>
    /// Sets the domain custom fields JSON.
    /// </summary>
    public void SetDomainCustomFieldsJson(string? json)
    {
        DomainCustomFieldsJson = json;
    }

    /// <summary>
    /// Sets the work item type code.
    /// </summary>
    public void SetWorkItemTypeCode(string? typeCode)
    {
        WorkItemTypeCode = typeCode;
    }

    /// <summary>
    /// Sets the review status.
    /// </summary>
    public void SetReviewStatus(ReviewStatus status)
    {
        ReviewStatus = status;
    }

    /// <summary>
    /// Sets the ticket type.
    /// </summary>
    public void SetTicketType(TicketType? type)
    {
        TicketType = type;
    }

    /// <summary>
    /// Adds a comment to the ticket.
    /// </summary>
    public void AddComment(TicketComment comment)
    {
        ((List<TicketComment>)Comments).Add(comment);
    }

    /// <summary>
    /// Adds a sub-ticket.
    /// </summary>
    public void AddSubTicket(Ticket subTicket)
    {
        ((List<Ticket>)SubTickets).Add(subTicket);
        subTicket.SetPropertyForSeeding(t => t.ParentTicketGuid = Guid);
    }

    // ═════════════════════════════════════════════════════════════════
    // QUERY METHODS (Pure functions for business logic)
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Determines if the ticket can be edited in its current state.
    /// </summary>
    public bool CanEditInCurrentState()
    {
        return TicketStatus is Common.Status.Pending or Common.Status.Assigned or Common.Status.InProgress;
    }

    /// <summary>
    /// Determines if the ticket can be edited by the specified user.
    /// </summary>
    public bool CanBeEditedBy(string userId, IEnumerable<string> userRoles)
    {
        if (userRoles.Contains(Constants.RoleAdmin))
            return true;

        if (userRoles.Contains(Constants.RoleCustomer))
            return CustomerId == userId;

        if (userRoles.Contains(Constants.RoleEmployee))
            return ResponsibleId == userId || ResponsibleId == null;

        return false;
    }

    /// <summary>
    /// Determines if the ticket can be assigned in its current state.
    /// </summary>
    public bool CanBeAssigned()
    {
        return TicketStatus is Common.Status.Pending or Common.Status.Assigned or Common.Status.InProgress;
    }

    /// <summary>
    /// Checks if a status transition is valid.
    /// </summary>
    public static bool IsValidTransition(Status from, Status to)
    {
        return from switch
        {
            Common.Status.Pending => to is Common.Status.Assigned or Common.Status.Cancelled or Common.Status.InProgress,
            Common.Status.Assigned => to is Common.Status.InProgress or Common.Status.Cancelled or Common.Status.Pending,
            Common.Status.InProgress => to is Common.Status.Completed or Common.Status.Cancelled or Common.Status.Assigned or Common.Status.Pending,
            Common.Status.Completed => to is Common.Status.InProgress, // Reopen
            Common.Status.Cancelled => to is Common.Status.Pending,   // Reactivate
            _ => false
        };
    }

    /// <summary>
    /// Gets valid transitions from a given status.
    /// </summary>
    public static string GetValidTransitions(Status from)
    {
        var transitions = from switch
        {
            Common.Status.Pending => new[] { Common.Status.Assigned, Common.Status.Cancelled, Common.Status.InProgress },
            Common.Status.Assigned => new[] { Common.Status.InProgress, Common.Status.Cancelled, Common.Status.Pending },
            Common.Status.InProgress => new[] { Common.Status.Completed, Common.Status.Cancelled, Common.Status.Assigned, Common.Status.Pending },
            Common.Status.Completed => new[] { Common.Status.InProgress },
            Common.Status.Cancelled => new[] { Common.Status.Pending },
            _ => Array.Empty<Status>()
        };

        return string.Join(", ", transitions.Select(t => t.ToString()));
    }

    /// <summary>
    /// Determines if a user can change the ticket status.
    /// </summary>
    public bool CanChangeStatus(string userId, IEnumerable<string> userRoles)
    {
        // Admins and employees can always change status
        if (userRoles.Contains(Constants.RoleAdmin) || userRoles.Contains(Constants.RoleEmployee))
            return true;

        // Customers can only cancel their own pending tickets
        if (userRoles.Contains(Constants.RoleCustomer))
        {
            return CustomerId == userId && TicketStatus == Common.Status.Pending;
        }

        return false;
    }

    /// <summary>
    /// Checks if the ticket is overdue based on completion target.
    /// </summary>
    public bool IsOverdue()
    {
        return CompletionTarget.HasValue &&
               CompletionTarget.Value < DateTime.UtcNow &&
               TicketStatus != Common.Status.Completed &&
               TicketStatus != Common.Status.Cancelled;
    }

    /// <summary>
    /// Gets the ticket age in days.
    /// </summary>
    public double GetAgeInDays()
    {
        return (DateTime.UtcNow - CreationDate).TotalDays;
    }

    // ═════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═════════════════════════════════════════════════════════════════

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
            Common.Status.Cancelled => "Cancelled",
            _ => "New"
        };
    }

    // ═════════════════════════════════════════════════════════════════
    // DOMAIN VALIDATION (Phase 3: Authorization & Rule Validation)
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates that the user can edit this ticket and throws DomainRuleException if not.
    /// This combines state-based and role-based authorization.
    /// </summary>
    /// <param name="userId">The user attempting to edit</param>
    /// <param name="userRoles">The roles of the user</param>
    /// <exception cref="DomainRuleException">Thrown when user is not authorized</exception>
    public void ValidateCanEdit(string userId, IEnumerable<string> userRoles)
    {
        if (!CanBeEditedBy(userId, userRoles))
        {
            throw new DomainRuleException("You are not authorized to edit this ticket.");
        }

        if (!CanEditInCurrentState())
        {
            throw new DomainRuleException(
                $"Tickets in {TicketStatus} status cannot be edited. " +
                "Only tickets in Pending, Assigned, or InProgress status can be edited.");
        }
    }

    /// <summary>
    /// Validates that the user can change the status and throws DomainRuleException if not.
    /// </summary>
    public void ValidateCanChangeStatus(string userId, IEnumerable<string> userRoles, Status targetStatus)
    {
        if (!CanChangeStatus(userId, userRoles))
        {
            throw new DomainRuleException("You are not authorized to change this ticket's status.");
        }

        if (!IsValidTransition(TicketStatus, targetStatus))
        {
            throw new DomainRuleException(
                $"Cannot transition from {TicketStatus} to {targetStatus}. " +
                $"Valid transitions: {GetValidTransitions(TicketStatus)}");
        }
    }

    /// <summary>
    /// Validates that the user can assign this ticket and throws DomainRuleException if not.
    /// </summary>
    public void ValidateCanAssign(string userId, IEnumerable<string> userRoles)
    {
        if (!userRoles.Contains(Constants.RoleAdmin) && !userRoles.Contains(Constants.RoleEmployee))
        {
            throw new DomainRuleException("Only administrators and employees can assign tickets.");
        }

        if (!CanBeAssigned())
        {
            throw new DomainRuleException(
                $"Tickets in {TicketStatus} status cannot be assigned. " +
                "Only tickets in Pending, Assigned, or InProgress status can be assigned.");
        }
    }

    /// <summary>
    /// Validates that the ticket can be viewed by the specified user.
    /// </summary>
    public bool CanBeViewedBy(string userId, IEnumerable<string> userRoles)
    {
        // Admins can view all tickets
        if (userRoles.Contains(Constants.RoleAdmin))
            return true;

        // Employees can view tickets they are assigned to or unassigned tickets
        if (userRoles.Contains(Constants.RoleEmployee))
            return ResponsibleId == userId || ResponsibleId == null || CustomerId == userId;

        // Customers can only view their own tickets
        if (userRoles.Contains(Constants.RoleCustomer))
            return CustomerId == userId;

        return false;
    }

    /// <summary>
    /// Validates view access and throws DomainRuleException if not authorized.
    /// </summary>
    public void ValidateCanView(string userId, IEnumerable<string> userRoles)
    {
        if (!CanBeViewedBy(userId, userRoles))
        {
            throw new DomainRuleException("You are not authorized to view this ticket.");
        }
    }

    /// <summary>
    /// Validates that required fields are present for the current state.
    /// Returns a list of validation errors, or empty if valid.
    /// </summary>
    public IEnumerable<string> ValidateRequiredFieldsForCurrentState()
    {
        var errors = new List<string>();

        // All tickets require a description
        if (string.IsNullOrWhiteSpace(Description))
            errors.Add("Description is required");

        // All tickets require a title
        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title is required");

        // Assigned/InProgress tickets require a responsible person
        if (TicketStatus is Common.Status.Assigned or Common.Status.InProgress &&
            string.IsNullOrEmpty(ResponsibleId))
        {
            errors.Add("A responsible agent is required for Assigned/InProgress tickets");
        }

        // InProgress tickets should have an estimated effort
        if (TicketStatus == Common.Status.InProgress && EstimatedEffortPoints <= 0)
        {
            errors.Add("Estimated effort points should be set when ticket is In Progress");
        }

        return errors;
    }

    /// <summary>
    /// Validates required fields and throws DomainException if any are missing.
    /// </summary>
    public void ValidateRequiredFieldsOrThrow()
    {
        var errors = ValidateRequiredFieldsForCurrentState();
        if (errors.Any())
        {
            throw new DomainException($"Validation failed: {string.Join("; ", errors)}");
        }
    }

    /// <summary>
    /// Gets a summary of the ticket's current state for debugging/logging.
    /// </summary>
    public string GetStateSummary()
    {
        return $"Ticket {Guid:N} | Status: {TicketStatus} | " +
               $"Customer: {CustomerId ?? "(none)"} | " +
               $"Responsible: {ResponsibleId ?? "(unassigned)"} | " +
               $"Age: {GetAgeInDays():F1} days";
    }

    // ═════════════════════════════════════════════════════════════════
    // LEGACY COMPATIBILITY (for gradual migration)
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// [LEGACY] Direct property setters for seeding and migration scenarios.
    /// These methods bypass domain validation and should only be used for:
    /// - Data seeding
    /// - Database migrations
    /// - Importing legacy data
    /// </summary>
    public void SetPropertyForSeeding(Action<Ticket> setter)
    {
        setter(this);
    }
}
