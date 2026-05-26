using TicketMasala.Domain.Common;

namespace TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

/// <summary>
/// Marker interface for all ticket lifecycle commands.
/// </summary>
public interface ITicketCommand { }

/// <summary>
/// Create a new ticket.
/// </summary>
public sealed record CreateTicketCommand(
    string Description,
    string CustomerId,
    string? ResponsibleId = null,
    Guid? ProjectGuid = null,
    DateTime? CompletionTarget = null
) : ITicketCommand;

/// <summary>
/// Resolve a ticket (mark as completed with billable amount).
/// </summary>
public sealed record ResolveTicketCommand(
    Guid TicketGuid,
    string ResolutionNotes,
    decimal? BillableAmount = null
) : ITicketCommand;

/// <summary>
/// Add a comment to a ticket.
/// </summary>
public sealed record AddCommentCommand(
    Guid TicketGuid,
    string Body,
    bool IsInternal = false
) : ITicketCommand;

/// <summary>
/// Log time worked on a ticket.
/// </summary>
public sealed record LogTimeCommand(
    Guid TicketGuid,
    double Hours,
    DateTime Date,
    string Description
) : ITicketCommand;

/// <summary>
/// Update an existing ticket.
/// </summary>
public sealed record UpdateTicketCommand(
    Guid TicketGuid,
    string Description,
    Status? NewStatus = null,
    string? ResponsibleId = null,
    Guid? ProjectGuid = null
) : ITicketCommand;

/// <summary>
/// Assign a ticket to an employee and/or project.
/// </summary>
public sealed record AssignTicketCommand(
    Guid TicketGuid,
    string? AgentId = null,
    Guid? ProjectGuid = null
) : ITicketCommand;

/// <summary>
/// Request a quality review for a ticket.
/// </summary>
public sealed record RequestReviewCommand(
    Guid TicketGuid
) : ITicketCommand;

/// <summary>
/// Submit a quality review for a ticket.
/// </summary>
public sealed record SubmitReviewCommand(
    Guid TicketGuid,
    int Score,
    string Feedback,
    bool Approved
) : ITicketCommand;

/// <summary>
/// Batch assign multiple tickets.
/// </summary>
public sealed record BatchAssignCommand(
    IReadOnlyList<Guid> TicketGuids,
    string? AgentId = null,
    Guid? ProjectGuid = null,
    bool UseGerdaRecommendations = false
) : ITicketCommand;
