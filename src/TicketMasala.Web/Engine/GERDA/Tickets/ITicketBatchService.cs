using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Batch service for bulk ticket operations.
/// Part of CQRS-lite pattern splitting from TicketService.
/// </summary>
public interface ITicketBatchService
{
    /// <summary>
    /// Batch assign tickets using GERDA recommendations or manual assignment
    /// </summary>
    Task<(int assigned, int failed)> BatchAssignTicketsAsync(
        List<Guid> ticketGuids,
        bool useGerdaRecommendations,
        string? manualAgentId = null);

    /// <summary>
    /// Batch assign multiple tickets to a single agent
    /// </summary>
    Task<int> BatchAssignToAgentAsync(List<Guid> ticketGuids, string agentId);

    /// <summary>
    /// Batch update status for multiple tickets
    /// </summary>
    Task<int> BatchUpdateStatusAsync(List<Guid> ticketGuids, Status newStatus);

}
