using TicketMasala.Domain.Common;

namespace TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

/// <summary>
/// Marker interface for all ticket lifecycle commands.
/// </summary>
public interface ITicketCommand { }

public sealed record CreateTicketCommand(
    string Description,
    string CustomerId,
    string? ResponsibleId = null,
    Guid? ProjectGuid = null,
    DateTime? CompletionTarget = null
) : ITicketCommand;

public sealed record ResolveTicketCommand(
    Guid TicketGuid,
    string ResolutionNotes,
    decimal? BillableAmount = null
) : ITicketCommand;

public sealed record AddCommentCommand(
    Guid TicketGuid,
    string Body,
    bool IsInternal = false
) : ITicketCommand;

public sealed record LogTimeCommand(
    Guid TicketGuid,
    double Hours,
    DateTime Date,
    string Description
) : ITicketCommand;

public sealed record UpdateTicketCommand(
    Guid TicketGuid,
    string Description,
    Status? NewStatus = null,
    string? ResponsibleId = null,
    Guid? ProjectGuid = null
) : ITicketCommand;

public sealed record AssignTicketCommand(
    Guid TicketGuid,
    string? AgentId = null,
    Guid? ProjectGuid = null
) : ITicketCommand;

public sealed record RequestReviewCommand(
    Guid TicketGuid
) : ITicketCommand;

public sealed record SubmitReviewCommand(
    Guid TicketGuid,
    int Score,
    string Feedback,
    bool Approved
) : ITicketCommand;

public sealed record BatchAssignCommand(
    IReadOnlyList<Guid> TicketGuids,
    string? AgentId = null,
    Guid? ProjectGuid = null,
    bool UseGerdaRecommendations = false
) : ITicketCommand;

public sealed record TransitionStatusCommand(
    Guid TicketGuid,
    Status NewStatus
) : ITicketCommand;
