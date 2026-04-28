// Skeleton implementation - will be filled in Phase 2
namespace TicketMasala.Web.Modules.Tickets;

internal class TicketModule : ITicketModule
{
    public Task<TicketResult<Guid>> CreateAsync(CreateTicketCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<TicketResult<Unit>> UpdateAsync(UpdateTicketCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<TicketResult<Unit>> AssignAsync(AssignTicketCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<TicketResult<Unit>> TransitionStatusAsync(TransitionStatusCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<TicketResult<TicketDetailsDto>> GetDetailsAsync(Guid ticketId, string requestingUserId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<TicketSearchResult> SearchAsync(TicketSearchQuery query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
