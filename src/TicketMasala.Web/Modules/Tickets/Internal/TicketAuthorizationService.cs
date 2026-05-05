using TicketMasala.Domain;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Modules.Tickets.Internal;

internal interface ITicketAuthorizationService
{
    bool CanEdit(Ticket ticket, string userId, IEnumerable<string> roles);
    bool CanAssign(Ticket ticket, IEnumerable<string> roles);
    bool CanChangeStatus(Ticket ticket, string userId, IEnumerable<string> roles, string targetStatus);
    bool CanView(Ticket ticket, string userId, IEnumerable<string> roles);
}

internal class TicketAuthorizationService : ITicketAuthorizationService
{
    public bool CanEdit(Ticket ticket, string userId, IEnumerable<string> roles)
        => ticket.CanBeEditedBy(userId, roles) && ticket.CanEditInCurrentState();

    public bool CanAssign(Ticket ticket, IEnumerable<string> roles)
    {
        // Must have role AND ticket must be in assignable state
        var hasRole = roles.Contains(Constants.RoleAdmin) || roles.Contains(Constants.RoleEmployee);
        return hasRole && ticket.CanBeAssigned();
    }

    public bool CanChangeStatus(Ticket ticket, string userId, IEnumerable<string> roles, string targetStatus)
    {
        // Check user can change status AND the specific transition is valid
        if (!ticket.CanChangeStatus(userId, roles))
            return false;

        if (!Enum.TryParse<Domain.Common.Status>(targetStatus, out var target))
            return false;

        return Ticket.IsValidTransition(ticket.TicketStatus, target);
    }

    public bool CanView(Ticket ticket, string userId, IEnumerable<string> roles)
        => ticket.CanBeViewedBy(userId, roles);
}
