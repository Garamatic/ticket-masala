using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Workflow;

/// <summary>
/// Domain port for workflow transition policy.
/// Answers "can ticket X transition to status Y for user Z?"
/// without leaking configuration or compilation details.
/// </summary>
public interface ITicketWorkflowPolicy
{
    /// <summary>
    /// Checks whether the given user may transition the ticket to the target status.
    /// </summary>
    /// <param name="ticket">The ticket to transition</param>
    /// <param name="targetStatus">Desired target status</param>
    /// <param name="context">User requesting the transition</param>
    /// <returns>True if the transition is permitted by workflow policy</returns>
    bool CanTransition(Ticket ticket, Status targetStatus, ITicketWorkflowContext context);

    /// <summary>
    /// Returns all statuses the ticket can transition to for the given user.
    /// </summary>
    /// <param name="ticket">The ticket</param>
    /// <param name="context">User requesting the list</param>
    /// <returns>Enumerable of permitted next statuses</returns>
    IEnumerable<Status> GetValidNextStates(Ticket ticket, ITicketWorkflowContext context);
}
