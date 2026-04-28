using System.Text.Json;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<TicketEditService> _logger;

    public TicketEditService(
        ITicketReadService ticketReadService,
        IRuleEngineService ruleEngine,
        ILogger<TicketEditService> logger)
    {
        _ticketReadService = ticketReadService;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    /// <summary>
    /// Deserializes custom fields JSON safely, returning empty dictionary on error.
    /// </summary>
    private Dictionary<string, object> DeserializeCustomFields(string? json, Guid ticketGuid)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, object>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                ?? new Dictionary<string, object>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize CustomFieldsJson for ticket {TicketGuid}", ticketGuid);
            return new Dictionary<string, object>();
        }
    }

    public async Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user)
    {
        var ticket = await _ticketReadService.GetTicketForEditAsync(ticketId).ConfigureAwait(false);
        if (ticket == null)
            return null;

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        // Authorize before loading dropdown lists to avoid wasted database calls
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID is required for authorization.");

        // All users must be authorized to edit this ticket
        var userRoles = user.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (!ticket.CanBeEditedBy(userId, userRoles))
            throw new UnauthorizedAccessException("You are not authorized to edit this ticket.");

        // Customers have additional restrictions: can only edit in specific states
        if (isCustomer && !ticket.CanEditInCurrentState())
        {
            throw new InvalidOperationException(
                $"You can only edit tickets that are in Pending, Assigned, or In Progress status. " +
                $"This ticket is currently {ticket.TicketStatus}.");
        }

        var responsibleUsers = await _ticketReadService.GetAllUsersSelectListAsync().ConfigureAwait(false);
        var customers = await _ticketReadService.GetCustomerSelectListAsync().ConfigureAwait(false);
        var projects = await _ticketReadService.GetProjectSelectListAsync().ConfigureAwait(false);

        var viewModel = new EditTicketViewModel
        {
            Guid = ticket.Guid,
            Description = ticket.Description,
            TicketStatus = ticket.TicketStatus,
            CompletionTarget = ticket.CompletionTarget,
            ResponsibleUserId = ticket.Responsible?.Id,
            CustomerId = ticket.CustomerId,
            ProjectGuid = ticket.ProjectGuid,
            ResponsibleUsers = responsibleUsers.ToList(),
            CustomerList = customers.ToList(),
            ProjectList = projects.ToList()
        };

        // Deserialize custom field values from ticket's JSON storage
        var customFieldValues = DeserializeCustomFields(ticket.CustomFieldsJson, ticket.Guid);

        // Get valid status transitions for the current ticket state
        var validStates = _ruleEngine.GetValidNextStates(ticket, user);
        var allowedStatuses = validStates.Union(new[] { ticket.TicketStatus }).Distinct().ToList();

        return new TicketEditContext
        {
            ViewModel = viewModel,
            DomainId = ticket.DomainId,
            WorkItemTypeCode = ticket.WorkItemTypeCode,
            CustomFieldValues = customFieldValues,
            ValidStatuses = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(allowedStatuses)
        };
    }

    public async Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user)
    {
        var context = new TicketEditContext();

        // Note: Dropdown lists (ResponsibleUsers, CustomerList, ProjectList) are populated
        // by GetCreateReloadContextAsync in the controller. This method only fetches
        // edit-specific data that varies based on the ticket state.

        var reloadTicket = await _ticketReadService.GetTicketForEditAsync(ticketId).ConfigureAwait(false);
        if (reloadTicket != null)
        {
            // Valid status transitions depend on current ticket state and user permissions
            var validStates = _ruleEngine.GetValidNextStates(reloadTicket, user);
            var allowedStatuses = validStates.Union(new[] { reloadTicket.TicketStatus }).Distinct().ToList();
            context.ValidStatuses = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(allowedStatuses);
            context.WorkItemTypeCode = reloadTicket.WorkItemTypeCode;

            // Preserve custom field values from ticket's JSON storage
            context.CustomFieldValues = DeserializeCustomFields(reloadTicket.CustomFieldsJson, reloadTicket.Guid);
        }

        return context;
    }
}
