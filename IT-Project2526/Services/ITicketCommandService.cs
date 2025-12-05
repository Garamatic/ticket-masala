using IT_Project2526.Models;

namespace IT_Project2526.Services;

/// <summary>
/// Command service for single ticket mutations.
/// Part of CQRS-lite pattern splitting from TicketService.
/// </summary>
public interface ITicketCommandService
{
    /// <summary>
    /// Create a new ticket with proper defaults and associations
    /// </summary>
    Task<Ticket> CreateTicketAsync(
        string description,
        string customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget);

    /// <summary>
    /// Update an existing ticket
    /// </summary>
    Task<bool> UpdateTicketAsync(
        Guid ticketGuid,
        string description,
        Status status,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget);

    /// <summary>
    /// Assign a ticket to an agent
    /// </summary>
    Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId);

    /// <summary>
    /// Assign a ticket to an agent and/or project (manager functionality)
    /// </summary>
    Task<bool> AssignTicketWithProjectAsync(
        Guid ticketGuid,
        string? agentId,
        Guid? projectGuid);

    /// <summary>
    /// Add a comment to a ticket
    /// </summary>
    Task<TicketComment> AddCommentAsync(
        Guid ticketId,
        string body,
        bool isInternal,
        string authorId);
}
