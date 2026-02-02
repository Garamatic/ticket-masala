using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.Facades;
using TicketMasala.Web.Common;

namespace TicketMasala.Web.Orchestrators;

public interface ITicketOrchestrator
{
    Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel, ClaimsPrincipal user);
    Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid id, ClaimsPrincipal user);
    Task<TicketDetailContext> GetTicketDetailContextAsync(TicketDetailsViewModel viewModel);
    Task<string> GenerateAiSummaryAsync(Guid ticketId);
    
    Task<TicketCreateContext> GetCreateContextAsync(Guid? projectGuid, ClaimsPrincipal user);
    Task<Result<Guid>> CreateTicketAsync(
        string description, 
        string customerId, 
        string? responsibleId, 
        Guid? projectGuid, 
        DateTime? completionTarget, 
        string? domainId, 
        string? workItemTypeCode, 
        IFormCollection form, 
        ClaimsPrincipal user);

    Task<TicketEditContext?> GetEditContextAsync(Guid id, ClaimsPrincipal user);
    Task<TicketEditContext> GetEditReloadContextAsync(Guid id, ClaimsPrincipal user);
    Task<Result> UpdateTicketAsync(Guid id, EditTicketViewModel viewModel, IFormCollection form, ClaimsPrincipal user);
}
