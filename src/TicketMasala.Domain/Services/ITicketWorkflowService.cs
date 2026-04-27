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
}
