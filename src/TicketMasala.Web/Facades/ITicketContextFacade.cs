using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Domain.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TicketMasala.Web.Facades;

public interface ITicketContextFacade
{
    // Existing Detail method
    Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketId, string? userId, bool isCustomer);
    Task<TicketDetailContext> GetTicketDetailContextAsync(TicketDetailsViewModel viewModel);

    // New Create method
    Task<TicketCreateContext> GetCreateContextAsync(bool isCustomer, string? preselectedCustomerId = null, Guid? projectGuid = null);

    // New Edit method
    Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user);
    Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user);
}
