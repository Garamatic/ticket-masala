namespace TicketMasala.Web.Modules.Tickets;

public record CreateTicketCommand(
    string Description,
    string CustomerId,
    string? ResponsibleId,
    Guid? ProjectGuid,
    DateTime? CompletionTarget,
    string? DomainId,
    string? WorkItemTypeCode,
    Dictionary<string, string> CustomFields,
    string CreatedByUserId);

public record UpdateTicketCommand(
    Guid TicketId,
    string Description,
    string TicketStatus,
    DateTime? CompletionTarget,
    string? CustomerId,
    Guid? ProjectGuid,
    Dictionary<string, string> CustomFields,
    string ModifiedByUserId,
    IReadOnlyList<string> ModifiedByRoles);

public record AssignTicketCommand(
    Guid TicketId,
    string ResponsibleId,
    string AssignedByUserId,
    IReadOnlyList<string> AssignedByRoles);

public record TransitionStatusCommand(
    Guid TicketId,
    string FromStatus,
    string ToStatus,
    string ChangedByUserId,
    IReadOnlyList<string> ChangedByRoles);
