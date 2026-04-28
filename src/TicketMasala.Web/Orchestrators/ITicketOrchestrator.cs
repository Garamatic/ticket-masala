using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TicketMasala.Web.Common;
using TicketMasala.Web.Facades;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Orchestrators;

/// <summary>
/// LEGACY: Orchestrator interface being consolidated into ITicketModule.
/// </summary>
/// <remarks>
/// P0 CONSOLIDATION: This interface is being migrated to ITicketModule deep module pattern.
/// New code should use ITicketModule exclusively. This interface will be removed in a future release.
/// </remarks>
[Obsolete("Use ITicketModule from TicketMasala.Web.Modules.Tickets instead. This interface will be removed in a future release.")]
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
