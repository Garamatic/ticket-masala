using TicketMasala.Domain.Common;

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
    string CreatedByUserId)
{
    /// <summary>
    /// Validates the command at construction time.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required fields are invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Description))
            throw new ArgumentException("Description is required", nameof(Description));

        if (Description.Length > 5000)
            throw new ArgumentException("Description cannot exceed 5000 characters", nameof(Description));

        if (string.IsNullOrWhiteSpace(CustomerId))
            throw new ArgumentException("CustomerId is required", nameof(CustomerId));
    }
}

/// <summary>
/// Command to update a ticket.
/// </summary>
/// <param name="TicketStatus">The status as a string (received from form/UI). Will be parsed to Status enum during processing.</param>
public record UpdateTicketCommand(
    Guid TicketId,
    string Description,
    string TicketStatus,
    DateTime? CompletionTarget,
    string? CustomerId,
    Guid? ProjectGuid,
    Dictionary<string, string> CustomFields,
    string ModifiedByUserId,
    IReadOnlyList<string> ModifiedByRoles)
{
    /// <summary>
    /// Attempts to parse the TicketStatus string to a valid Status enum.
    /// </summary>
    /// <returns>The parsed Status value, or null if parsing fails.</returns>
    public Status? TryParseStatus()
    {
        return Enum.TryParse<Status>(TicketStatus, out var status) ? status : null;
    }

    /// <summary>
    /// Validates that the TicketStatus can be parsed to a valid Status enum.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when status is invalid.</exception>
    public Status ParseStatusOrThrow()
    {
        return TryParseStatus()
            ?? throw new InvalidOperationException($"Invalid status: {TicketStatus}");
    }
}

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
