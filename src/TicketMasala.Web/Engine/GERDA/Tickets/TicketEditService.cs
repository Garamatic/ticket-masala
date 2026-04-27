using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Facades;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service for ticket edit view operations.
/// Single responsibility: Edit view concerns only.
/// </summary>
public interface ITicketEditService
{
    Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user);
    Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user);
}

public class TicketEditService : ITicketEditService
{
    private readonly ITicketReadService _ticketReadService;
    private readonly IRuleEngineService _ruleEngine;

    public TicketEditService(
        ITicketReadService ticketReadService,
        IRuleEngineService ruleEngine)
    {
        _ticketReadService = ticketReadService;
        _ruleEngine = ruleEngine;
    }

    public async Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user)
    {
        var ticket = await _ticketReadService.GetTicketForEditAsync(ticketId);
        if (ticket == null)
            return null;

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        if (isCustomer)
        {
            if (ticket.CustomerId != userId)
                throw new UnauthorizedAccessException("Customer is not authorized to edit this ticket.");

            if (ticket.TicketStatus != Status.Pending && ticket.TicketStatus != Status.Assigned)
            {
                throw new InvalidOperationException("You can only edit tickets that are in Pending or Assigned status.");
            }
        }

        var responsibleUsers = await _ticketReadService.GetAllUsersSelectListAsync();

        var viewModel = new EditTicketViewModel
        {
            Guid = ticket.Guid,
            Description = ticket.Description,
            TicketStatus = ticket.TicketStatus,
            CompletionTarget = ticket.CompletionTarget,
            ResponsibleUserId = ticket.Responsible?.Id,
            CustomerId = ticket.CustomerId,
            ProjectGuid = ticket.ProjectGuid,
            ResponsibleUsers = responsibleUsers,
            CustomerList = (await _ticketReadService.GetCustomerSelectListAsync()).ToList(),
            ProjectList = (await _ticketReadService.GetProjectSelectListAsync()).ToList()
        };

        return new TicketEditContext
        {
            ViewModel = viewModel,
            WorkItemTypeCode = ticket.WorkItemTypeCode
        };
    }

    public async Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user)
    {
        var context = new TicketEditContext();

        var reloadTicket = await _ticketReadService.GetTicketForEditAsync(ticketId);
        if (reloadTicket != null)
        {
            var validStates = _ruleEngine.GetValidNextStates(reloadTicket, user);
            var allowedStatuses = validStates.Union(new[] { reloadTicket.TicketStatus }).Distinct().ToList();
            context.ValidStatuses = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(allowedStatuses);
        }

        return context;
    }
}
