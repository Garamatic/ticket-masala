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
        => roles.Contains("Admin") || roles.Contains("Employee");

    public bool CanChangeStatus(Ticket ticket, string userId, IReadOnlyList<string> roles, string targetStatus)
        => ticket.CanChangeStatus(userId, roles);

    public bool CanView(Ticket ticket, string userId, IReadOnlyList<string> roles)
        => ticket.CanBeViewedBy(userId, roles);
}
