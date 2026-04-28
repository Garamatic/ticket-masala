using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Modules.Tickets.Internal;

internal interface ITicketAuthorizationService
{
    bool CanEdit(Ticket ticket, string userId, IReadOnlyList<string> roles);
    bool CanAssign(Ticket ticket, string userId, IReadOnlyList<string> roles);
    bool CanChangeStatus(Ticket ticket, string userId, IReadOnlyList<string> roles, string targetStatus);
    bool CanView(Ticket ticket, string userId, IReadOnlyList<string> roles);
}

internal class TicketAuthorizationService : ITicketAuthorizationService
{
    public bool CanEdit(Ticket ticket, string userId, IReadOnlyList<string> roles)
        => ticket.CanBeEditedBy(userId, roles) && ticket.CanEditInCurrentState();

    public bool CanAssign(Ticket ticket, string userId, IReadOnlyList<string> roles)
    {
        // Must have role AND ticket must be in assignable state
        var hasRole = roles.Contains("Admin") || roles.Contains("Employee");
        return hasRole && ticket.CanBeAssigned();
    }

    public bool CanChangeStatus(Ticket ticket, string userId, IReadOnlyList<string> roles, string targetStatus)
    {
        // Check user can change status AND the specific transition is valid
        if (!ticket.CanChangeStatus(userId, roles))
            return false;

        if (!Enum.TryParse<Domain.Common.Status>(targetStatus, out var target))
            return false;

        return Ticket.IsValidTransition(ticket.TicketStatus, target);
    }

    public bool CanView(Ticket ticket, string userId, IReadOnlyList<string> roles)
        => ticket.CanBeViewedBy(userId, roles);
}
