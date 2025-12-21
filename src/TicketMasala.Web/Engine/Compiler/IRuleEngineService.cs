using System.Security.Claims;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;

namespace TicketMasala.Web.Engine.Compiler;

public interface IRuleEngineService
{
    /// <summary>
    /// checks if a transition from current state to target state is allowed for the user
    /// </summary>
    bool CanTransition(Ticket ticket, Status targetStatus, ClaimsPrincipal user);

    /// <summary>
    /// Gets a list of valid next states for the ticket based on current state and user roles
    /// </summary>
    IEnumerable<Status> GetValidNextStates(Ticket ticket, ClaimsPrincipal user);

    /// <summary>
    /// Validates that all required fields for the target state are present
    /// </summary>
    /// <returns>List of missing field names</returns>
    IEnumerable<string> ValidateRequiredFields(Ticket ticket, Status targetStatus);
}
