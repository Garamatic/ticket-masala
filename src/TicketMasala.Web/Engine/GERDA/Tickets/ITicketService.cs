using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service responsible for ticket core business logic (CRUD).
/// [DEPRECATED] Use ITicketReadService and ITicketWorkflowService instead.
/// This interface is kept temporarily for binary compatibility if needed, but all methods have been moved.
/// </summary>
public interface ITicketService
{
    // All methods moved to ITicketReadService and ITicketWorkflowService
}
