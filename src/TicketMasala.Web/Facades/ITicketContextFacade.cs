using Microsoft.AspNetCore.Mvc.Rendering;
using TicketMasala.Domain.Configuration;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Facades;

public interface ITicketContextFacade
{
    // Existing Detail method
    Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketId, string? userId, bool isCustomer, CancellationToken ct = default);
    Task<TicketDetailContext> GetTicketDetailContextAsync(TicketDetailsViewModel viewModel);

    // New Create method
    Task<TicketCreateContext> GetCreateContextAsync(bool isCustomer, string? preselectedCustomerId = null, Guid? projectGuid = null, CancellationToken ct = default);

    // New Edit method
    Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct = default);
    Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct = default);
}
