using System.ComponentModel.DataAnnotations.Schema;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Events;
using TicketMasala.Domain.Exceptions;

namespace TicketMasala.Domain.Entities;

public partial class Ticket : BaseModel, IAggregateRoot, IHasDomainEvents
{
    public TicketType? TicketType { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime? CompletionTarget { get; set; }
    public DateTime? CompletionDate { get; set; }

    public int EstimatedEffortPoints { get; set; } = 0;

    /// <summary>
    /// Runtime priority score calculated by GERDA AI (0-100 scale).
    /// This is the primary property for business logic and display.
    /// For database queries, use ComputedPriority which is generated from CustomFieldsJson.
    /// </summary>
    public double PriorityScore { get; set; } = 0.0;

    [StringLength(1000)]
    public string? GerdaTags { get; set; } // Comma-separated: "AI-Dispatched,Spam-Cluster"

    public string? RecommendedProjectName { get; set; }
    public string? CurrentProjectName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? BillableAmount { get; set; }

    [StringLength(2000)]
    public string? ResolutionNotes { get; set; }

    public string DomainId { get; set; } = "IT"; // TODO: Migrate callers to SetDomain()

    public string Status { get; set; } = "New";

    public Status TicketStatus { get; set; } = Common.Status.Pending; // TODO: Migrate callers to TransitionTo()

    public string Title { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? ContentHash { get; set; }

    [MaxLength(50)]
    public string? ConfigVersionId { get; set; }

    [Column(TypeName = "TEXT")]
    public string CustomFieldsJson { get; set; } = "{}";

    public double? ComputedPriority { get; private set; }
    public string? ComputedCategory { get; private set; }

    [StringLength(50)]
    public string? WorkItemTypeCode { get; set; }

    [Column(TypeName = "TEXT")]
    public string? DomainCustomFieldsJson { get; set; }

    public Guid? ParentTicketGuid { get; set; }
    public string? CustomerId { get; set; }
    public Guid? ProjectGuid { get; set; }
    public string? ResponsibleId { get; set; }
    public List<string> WatcherIds { get; set; } = new List<string>();

    public Guid? SolvedByArticleId { get; set; }

    [StringLength(2000)]
    public string? AiSummary { get; set; }

    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.None;

    public virtual Project? Project { get; set; }
    public virtual ApplicationUser? Customer { get; set; }
    public virtual Employee? Responsible { get; set; }
    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public virtual ICollection<Ticket> SubTickets { get; set; } = new List<Ticket>();
    public virtual Ticket? ParentTicket { get; set; }

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// Do not use directly - use Ticket.Create() factory method instead.
    /// </summary>
    public Ticket()
    {
        SyncStatus();
    }

    /// <summary>Creates a new ticket with validation and domain events.</summary>
    public static Ticket Create(
        string description,
        string title,
        string? customerId,
        string? domainId = null,
        Guid? projectGuid = null,
        string? workItemTypeCode = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Ticket description is required");

        if (description.Length > 5000)
            throw new DomainException("Description cannot exceed 5000 characters");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Ticket title is required");

        if (title.Length > 200)
            throw new DomainException("Title cannot exceed 200 characters");

        var ticket = new Ticket
        {
            Description = description.Trim(),
            Title = title.Trim(),
            CustomerId = customerId,
            DomainId = domainId?.Trim() ?? "IT",
            ProjectGuid = projectGuid,
            WorkItemTypeCode = workItemTypeCode?.Trim(),
            TicketStatus = Common.Status.Pending,
            PriorityScore = 0.0,
            CustomFieldsJson = "{}"
        };

        ticket.SyncStatus();

        if (!string.IsNullOrEmpty(customerId))
        {
            ticket.RaiseDomainEvent(new TicketCreatedEvent(ticket.Guid, customerId, ticket.DomainId));
        }

        return ticket;
    }

    /// <summary>Creates a ticket from portal submission with minimal required fields.</summary>
    public static Ticket CreateFromPortal(
        string description,
        string? customerId,
        double? priorityScore = null,
        string? tags = null,
        DateTime? completionTarget = null)
    {
        var title = description.Length > 50
            ? description[..47] + "..."
            : description;

        var ticket = new Ticket
        {
            Description = description.Trim(),
            Title = title.Trim(),
            CustomerId = customerId,
            DomainId = "IT",
            TicketStatus = Common.Status.Pending,
            PriorityScore = 0.0,
            CustomFieldsJson = "{}"
        };

        ticket.SyncStatus();

        if (priorityScore.HasValue)
            ticket.SetPriorityScore(priorityScore.Value);

        if (!string.IsNullOrEmpty(tags))
            ticket.SetGerdaTags(tags);

        if (completionTarget.HasValue)
            ticket.SetCompletionTarget(completionTarget.Value);

        if (!string.IsNullOrEmpty(customerId))
        {
            ticket.RaiseDomainEvent(new TicketCreatedEvent(ticket.Guid, customerId, ticket.DomainId));
        }

        return ticket;
    }

    /// <summary>Updates the ticket description with validation.</summary>
    public void UpdateDescription(string newDescription, string updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            throw new DomainException("Description cannot be empty");

        if (newDescription.Length > 5000)
            throw new DomainException("Description cannot exceed 5000 characters");

        Description = newDescription.Trim();
        LastModified = DateTime.UtcNow;

        RaiseDomainEvent(new TicketUpdatedEvent(Guid, nameof(Description), updatedByUserId));
    }

    /// <summary>Updates the ticket title with validation.</summary>
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

    /// <summary>Assigns the ticket to an employee, raising domain events.</summary>
    public void AssignTo(string responsibleId, string assignedByUserId)
    {
        if (string.IsNullOrWhiteSpace(responsibleId))
            throw new DomainException("Responsible ID is required for assignment");

        var oldResponsibleId = ResponsibleId;
        ResponsibleId = responsibleId;

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

    public const string UnassignedIndicator = "(unassigned)";

    /// <summary>Unassigns the ticket.</summary>
    public void Unassign(string unassignedByUserId)
    {
        var oldResponsibleId = ResponsibleId;
        ResponsibleId = null;

        RaiseDomainEvent(new TicketAssignedEvent(Guid, UnassignedIndicator, oldResponsibleId, unassignedByUserId));
        LastModified = DateTime.UtcNow;
    }

    /// <summary>Transitions the ticket to a new status with state machine validation.</summary>
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

        if (newStatus == Common.Status.Completed && !CompletionDate.HasValue)
        {
            CompletionDate = DateTime.UtcNow;
        }

        if (oldStatus == Common.Status.Completed && newStatus != Common.Status.Completed)
        {
            CompletionDate = null;
        }

        RaiseDomainEvent(new TicketStatusChangedEvent(Guid, oldStatus, newStatus, changedByUserId));
        LastModified = DateTime.UtcNow;
    }


    public void SetCompletionDate(DateTime? completionDate)
    {
        CompletionDate = completionDate;
    }

    /// <summary>Resolves the ticket, transitions to Completed, and raises TicketResolvedEvent.</summary>
    public void Resolve(string resolutionNotes, decimal? billableAmount, string resolvedByUserId)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotes))
            throw new DomainException("Resolution notes are required");

        if (resolutionNotes.Length > 2000)
            throw new DomainException("Resolution notes cannot exceed 2000 characters");

        if (billableAmount.HasValue && billableAmount.Value < 0)
            throw new DomainException("Billable amount cannot be negative");

        if (TicketStatus == Common.Status.Completed)
            throw new DomainException("Ticket is already completed. Cannot resolve again.");

        ResolutionNotes = resolutionNotes.Trim();
        BillableAmount = billableAmount;

        TransitionTo(Common.Status.Completed, resolvedByUserId);
        RaiseDomainEvent(new TicketResolvedEvent(
            Guid,
            CustomerId ?? "(unknown)",
            BillableAmount,
            ResolutionNotes,
            DateTime.UtcNow,
            resolvedByUserId
        ));
    }

    /// <summary>Updates custom fields JSON with validation.</summary>
    public void UpdateCustomFields(string customFieldsJson, string updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(customFieldsJson))
            throw new DomainException("Custom fields JSON cannot be empty");

        CustomFieldsJson = customFieldsJson;
        LastModified = DateTime.UtcNow;

        RaiseDomainEvent(new TicketUpdatedEvent(Guid, nameof(CustomFieldsJson), updatedByUserId));
    }

    /// <summary>Adds a tag to the GerdaTags collection.</summary>
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

    /// <summary>Sets the GERDA AI summary.</summary>
    public void SetAiSummary(string? summary)
    {
        if (!string.IsNullOrEmpty(summary) && summary.Length > 2000)
            throw new DomainException("AI summary cannot exceed 2000 characters");

        AiSummary = summary;
        LastModified = DateTime.UtcNow;
    }

    /// <summary>Sets the priority score (0-100).</summary>
    public void SetPriorityScore(double score)
    {
        PriorityScore = Math.Clamp(score, 0.0, 100.0);
    }


    public void SetCompletionTarget(DateTime target)
    {
        CompletionTarget = target;
    }


    public void SetContentHash(string? hash)
    {
        ContentHash = hash;
    }


    public void SetConfigVersionId(string? configVersionId)
    {
        ConfigVersionId = configVersionId;
    }

    /// <summary>Sets GERDA tags (replaces existing).</summary>
    public void SetGerdaTags(string? tags)
    {
        GerdaTags = tags;
    }

    /// <summary>Sets the estimated effort points.</summary>
    public void SetEstimatedEffortPoints(int points)
    {
        EstimatedEffortPoints = Math.Max(0, points);
    }


    public void SetProject(Guid? projectGuid)
    {
        ProjectGuid = projectGuid;
    }

    /// <summary>Sets the domain ID (e.g., "IT", "LEGAL").</summary>
    public void SetDomain(string domainId)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new DomainException("Domain ID cannot be empty");

        DomainId = domainId.Trim();
    }


    public void SetCustomer(string? customerId)
    {
        CustomerId = customerId;
    }


    public void SetParentTicket(Guid? parentTicketGuid)
    {
        ParentTicketGuid = parentTicketGuid;
    }

    /// <summary>Sets the responsible employee ID directly (bypasses domain events).</summary>
    public void SetResponsibleId(string? responsibleId)
    {
        ResponsibleId = responsibleId;
    }

    /// <summary>Sets the responsible employee navigation property.</summary>
    public void SetResponsible(Employee? responsible)
    {
        Responsible = responsible;
        if (responsible != null)
        {
            ResponsibleId = responsible.Id;
        }
    }


    public void SetDomainCustomFieldsJson(string? json)
    {
        DomainCustomFieldsJson = json;
    }


    public void SetWorkItemTypeCode(string? typeCode)
    {
        WorkItemTypeCode = typeCode;
    }

    /// <summary>Sets the review status.</summary>
    public void SetReviewStatus(ReviewStatus status)
    {
        ReviewStatus = status;
    }


    public void SetTicketType(TicketType? type)
    {
        TicketType = type;
    }

    /// <summary>Adds a comment to the ticket.</summary>
    public void AddComment(TicketComment comment)
    {
        Comments.Add(comment);
    }

    /// <summary>Adds a sub-ticket.</summary>
    public void AddSubTicket(Ticket subTicket)
    {
        SubTickets.Add(subTicket);
        subTicket.SetParentTicket(Guid);
    }

    /// <summary>Records that child tickets were grouped under this ticket.</summary>
    public void RecordChildrenGrouped(IEnumerable<Ticket> childTickets, string groupedByUserId)
    {
        var childGuids = childTickets.Select(t => t.Guid).ToList();
        RaiseDomainEvent(new TicketGroupedEvent(Guid, childGuids, groupedByUserId));
    }

    /// <summary>Records that this ticket was ungrouped from its parent.</summary>
    public void RecordUngrouped(Guid? formerParentGuid, string ungroupedByUserId)
    {
        RaiseDomainEvent(new TicketUngroupedEvent(Guid, formerParentGuid, ungroupedByUserId));
    }

    /// <summary>Determines if the ticket can be edited in its current state.</summary>
    public bool CanEditInCurrentState()
    {
        return TicketStatus is Common.Status.Pending or Common.Status.Assigned or Common.Status.InProgress;
    }

    /// <summary>Determines if the ticket can be edited by the specified user.</summary>
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

    /// <summary>Determines if the ticket can be assigned in its current state.</summary>
    public bool CanBeAssigned()
    {
        return TicketStatus is Common.Status.Pending or Common.Status.Assigned or Common.Status.InProgress;
    }

    /// <summary>Checks if a status transition is valid.</summary>
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

    /// <summary>Gets valid transitions from a given status.</summary>
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

    /// <summary>Determines if a user can change the ticket status.</summary>
    public bool CanChangeStatus(string userId, IEnumerable<string> userRoles)
    {
        if (userRoles.Contains(Constants.RoleAdmin) || userRoles.Contains(Constants.RoleEmployee))
            return true;

        if (userRoles.Contains(Constants.RoleCustomer))
            return CustomerId == userId && TicketStatus == Common.Status.Pending;

        return false;
    }

    // Workflow policy integration

    /// <summary>Determines if the user may transition this ticket to the target status.</summary>
    public bool CanTransitionTo(Status targetStatus, Workflow.ITicketWorkflowPolicy policy, Workflow.ITicketWorkflowContext context)
    {
        return policy.CanTransition(this, targetStatus, context);
    }

    /// <summary>Returns all statuses this ticket may transition to for the given user.</summary>
    public IEnumerable<Status> GetValidNextStates(Workflow.ITicketWorkflowPolicy policy, Workflow.ITicketWorkflowContext context)
    {
        return policy.GetValidNextStates(this, context);
    }

    /// <summary>Checks if the ticket is overdue based on completion target.</summary>
    public bool IsOverdue()
    {
        return CompletionTarget.HasValue &&
               CompletionTarget.Value < DateTime.UtcNow &&
               TicketStatus != Common.Status.Completed &&
               TicketStatus != Common.Status.Cancelled;
    }

    /// <summary>Gets the ticket age in days.</summary>
    public double GetAgeInDays()
    {
        return (DateTime.UtcNow - CreationDate).TotalDays;
    }

    /// <summary>Ensures Status and TicketStatus remain synchronized.</summary>
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

    /// <summary>Validates that the user can edit this ticket.</summary>
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

    /// <summary>Validates that the user can change the status.</summary>
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

    /// <summary>Validates that the user can assign this ticket.</summary>
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

    /// <summary>Validates that the ticket can be viewed by the specified user.</summary>
    public bool CanBeViewedBy(string userId, IEnumerable<string> userRoles)
    {
        if (userRoles.Contains(Constants.RoleAdmin))
            return true;

        if (userRoles.Contains(Constants.RoleEmployee))
            return ResponsibleId == userId || ResponsibleId == null || CustomerId == userId;

        if (userRoles.Contains(Constants.RoleCustomer))
            return CustomerId == userId;

        return false;
    }

    /// <summary>Validates view access and throws if not authorized.</summary>
    public void ValidateCanView(string userId, IEnumerable<string> userRoles)
    {
        if (!CanBeViewedBy(userId, userRoles))
        {
            throw new DomainRuleException("You are not authorized to view this ticket.");
        }
    }

    /// <summary>Validates that required fields are present for the current state.</summary>
    public IEnumerable<string> ValidateRequiredFieldsForCurrentState()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Description))
            errors.Add("Description is required");

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title is required");

        if (TicketStatus is Common.Status.Assigned or Common.Status.InProgress &&
            string.IsNullOrEmpty(ResponsibleId))
        {
            errors.Add("A responsible agent is required for Assigned/InProgress tickets");
        }

        if (TicketStatus == Common.Status.InProgress && EstimatedEffortPoints <= 0)
            errors.Add("Estimated effort points should be set when ticket is In Progress");

        return errors;
    }

    /// <summary>Validates required fields and throws if any are missing.</summary>
    public void ValidateRequiredFieldsOrThrow()
    {
        var errors = ValidateRequiredFieldsForCurrentState();
        if (errors.Any())
        {
            throw new DomainException($"Validation failed: {string.Join("; ", errors)}");
        }
    }

    /// <summary>Gets a summary of the ticket's current state.</summary>
    public string GetStateSummary()
    {
        return $"Ticket {Guid:N} | Status: {TicketStatus} | " +
               $"Customer: {CustomerId ?? "(none)"} | " +
               $"Responsible: {ResponsibleId ?? UnassignedIndicator} | " +
               $"Age: {GetAgeInDays():F1} days";
    }

    /// <summary>
    /// [LEGACY] Direct property setters for seeding and migration scenarios only.
    /// Bypasses ALL domain validation. Disallowed in production unless
    /// TICKETMASALA_ALLOW_SEED_BYPASS=true is set.
    /// </summary>
    public void SetPropertyForSeeding(Action<Ticket> setter)
    {
#if !DEBUG && !TESTING
        // In production, require explicit opt-in via environment variable
        var allowSeedBypass = Environment.GetEnvironmentVariable("TICKETMASALA_ALLOW_SEED_BYPASS") == "true";
        if (!allowSeedBypass)
        {
            throw new InvalidOperationException(
                "SetPropertyForSeeding is not allowed in production. " +
                "Set TICKETMASALA_ALLOW_SEED_BYPASS=true only for data migrations.");
        }
#endif

        setter(this);

        // After seeding, ensure status is synchronized (in case TicketStatus was modified)
        SyncStatus();
    }
}
