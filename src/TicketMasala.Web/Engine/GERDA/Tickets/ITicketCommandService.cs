using TicketMasala.Web.Models;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.ViewModels.GERDA;

namespace TicketMasala.Web.Services.Tickets;

/// <summary>
/// Command service for ticket write operations.
/// Follows CQRS pattern - separates write concerns from read concerns.
/// </summary>
public interface ITicketCommandService
{
    /// <summary>
    /// Create a new ticket with proper defaults and associations
    /// Notifies observers after creation (triggers GERDA processing)
    /// </summary>
    Task<Ticket> CreateTicketAsync(string description, string customerId, string? responsibleId, Guid? projectGuid, DateTime? completionTarget);

    /// <summary>
    /// Assign a ticket to an agent
    /// Notifies observers after assignment
    /// </summary>
    Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId);

    /// <summary>
    /// Assign a ticket to an agent and/or project (manager functionality)
    /// </summary>
    Task<bool> AssignTicketWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid);

    /// <summary>
    /// Update an existing ticket
    /// </summary>
    Task<bool> UpdateTicketAsync(Ticket ticket);

    /// <summary>
    /// Batch assign tickets using GERDA recommendations or manual assignment
    /// </summary>
    Task<BatchAssignResult> BatchAssignTicketsAsync(BatchAssignRequest request, Func<Guid, Task<string?>> getRecommendedAgent);

    /// <summary>
    /// Batch assign tickets to a specific agent
    /// </summary>
    Task BatchAssignToAgentAsync(List<Guid> ticketIds, string agentId);

    /// <summary>
    /// Batch update ticket statuses
    /// </summary>
    Task BatchUpdateStatusAsync(List<Guid> ticketIds, Status status);

    /// <summary>
    /// Add a comment to a ticket
    /// </summary>
    Task<TicketComment> AddCommentAsync(Guid ticketId, string body, bool isInternal, string authorId);

}
