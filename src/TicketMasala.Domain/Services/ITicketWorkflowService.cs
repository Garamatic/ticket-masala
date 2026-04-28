using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Services;

/// <summary>
/// Core ticket workflow operations available to all services.
/// </summary>
public interface ITicketWorkflowService
{
    Task<Ticket> CreateTicketAsync(
        string description,
        string customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget);

    Task<bool> UpdateTicketAsync(Ticket ticket);
    Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId);
    Task<bool> AssignTicketWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid);
    Task<TicketComment> AddCommentAsync(Guid ticketId, string body, bool isInternal, string authorId);
    Task<bool> RequestReviewAsync(Guid ticketId, string requesterId);
    Task<bool> SubmitReviewAsync(Guid ticketId, int score, string feedback, bool approved, string reviewerId);
    Task<TimeLog> LogTimeAsync(Guid ticketId, string userId, double hours, DateTime date, string description);

    /// <summary>
    /// Resolves a ticket with billable amount and resolution notes.
    /// Creates an outbox message for the ticket.resolved event.
    /// </summary>
    /// <param name="ticketGuid">The ticket GUID to resolve</param>
    /// <param name="resolutionNotes">Notes about how the ticket was resolved</param>
    /// <param name="billableAmount">Optional billable amount</param>
    /// <param name="resolvedByUserId">The ID of the user resolving the ticket</param>
    /// <returns>The resolved ticket</returns>
    Task<Ticket> ResolveTicketAsync(
        Guid ticketGuid,
        string resolutionNotes,
        decimal? billableAmount,
        string resolvedByUserId);
}
