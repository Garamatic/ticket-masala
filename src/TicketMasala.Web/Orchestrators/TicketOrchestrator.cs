using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Exceptions;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.AI;
using TicketMasala.Web.Common;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Facades;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Orchestrators;

[Obsolete("Use ITicketModule from TicketMasala.Web.Modules.Tickets instead. This orchestrator will be removed in a future release.")]
public class TicketOrchestrator : ITicketOrchestrator
{
    private readonly IGerda _gerda;
    private readonly ITicketWorkflowService _ticketWorkflowService;
    private readonly ITicketReadService _ticketReadService;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly IOpenAiService _openAiService;
    private readonly ITicketContextFacade _ticketContextFacade;
    private readonly ISavedFilterService _savedFilterService;
    private readonly ILogger<TicketOrchestrator> _logger;

    public TicketOrchestrator(
        IGerda gerda,
        ITicketWorkflowService ticketWorkflowService,
        ITicketReadService ticketReadService,
        IDomainConfigurationService domainConfig,
        IOpenAiService openAiService,
        ITicketContextFacade ticketContextFacade,
        ISavedFilterService savedFilterService,
        ILogger<TicketOrchestrator> logger)
    {
        _gerda = gerda;
        _ticketWorkflowService = ticketWorkflowService;
        _ticketReadService = ticketReadService;
        _domainConfig = domainConfig;
        _openAiService = openAiService;
        _ticketContextFacade = ticketContextFacade;
        _savedFilterService = savedFilterService;
        _logger = logger;
    }

    public async Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel, ClaimsPrincipal user)
    {
        if (searchModel == null)
            searchModel = new TicketSearchViewModel();

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        if (isCustomer && !string.IsNullOrEmpty(userId))
            searchModel.CustomerId = userId;

        var result = await _ticketReadService.SearchTicketsAsync(searchModel);
        result.Customers = await _ticketReadService.GetCustomerSelectListAsync();
        result.Employees = await _ticketReadService.GetEmployeeSelectListAsync();
        result.Projects = await _ticketReadService.GetProjectSelectListAsync();

        if (!string.IsNullOrEmpty(userId))
        {
            var filters = await _savedFilterService.GetFiltersForUserAsync(userId);
            result.SavedFilters = filters;
        }

        return result;
    }

    public async Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid id, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        return await _ticketContextFacade.GetTicketDetailsAsync(id, userId, isCustomer);
    }

    public async Task<TicketDetailContext> GetTicketDetailContextAsync(TicketDetailsViewModel viewModel)
    {
        return await _ticketContextFacade.GetTicketDetailContextAsync(viewModel);
    }

    public async Task<string> GenerateAiSummaryAsync(Guid ticketId)
    {
        var ticket = await _ticketReadService.GetTicketDetailsAsync(ticketId);
        if (ticket == null)
            throw new ArgumentException("Ticket not found");

        var query = $"Title: {ticket.Description} (Created: {ticket.CreationDate})\n\n" +
                $"Status: {ticket.TicketStatus}\n\n" +
                $"Discussion:\n" +
                string.Join("\n", ticket.Comments.OrderBy(c => c.CreatedAt).Select(c => $"- {c.Author?.Name ?? c.Author?.UserName ?? "Unknown"} ({c.CreatedAt}): {c.Body}"));

        try
        {
            return await _openAiService.GetResponseAsync(OpenAIPrompts.Summary, query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI summary for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<TicketCreateContext> GetCreateContextAsync(Guid? projectGuid, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        return await _ticketContextFacade.GetCreateContextAsync(isCustomer, userId, projectGuid);
    }

    public async Task<Result<Guid>> CreateTicketAsync(
        string description,
        string customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget,
        string? domainId,
        string? workItemTypeCode,
        IFormCollection form,
        ClaimsPrincipal user)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure<Guid>("Description is required.");
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        if (isCustomer && !string.IsNullOrEmpty(userId))
        {
            customerId = userId;
        }

        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Result.Failure<Guid>("Customer is required.");
        }

        try
        {
            var ticket = await _ticketWorkflowService.CreateTicketAsync(description, customerId, responsibleId, projectGuid, completionTarget);

            ticket.DomainId = domainId ?? _domainConfig.GetDefaultDomainId();
            ticket.WorkItemTypeCode = workItemTypeCode;

            var formDictionary = form.ToDictionary(x => x.Key, x => x.Value.ToString());
            ticket.CustomFieldsJson = _ticketReadService.ParseCustomFields(ticket.DomainId, formDictionary);

            await _ticketWorkflowService.UpdateTicketAsync(ticket);

            _logger.LogInformation("Processing ticket {TicketGuid} with GERDA AI (Domain: {DomainId}, Type: {WorkItemTypeCode})",
                ticket.Guid, ticket.DomainId, ticket.WorkItemTypeCode);
            await _gerda.ProcessAsync(ticket.Guid);

            var entityLabel = _domainConfig.GetEntityLabels(ticket.DomainId).WorkItem;
            _logger.LogInformation("GERDA processing completed for ticket {TicketGuid}", ticket.Guid);

            return Result.Success(ticket.Guid, $"{entityLabel} created successfully! GERDA AI has processed the {entityLabel.ToLower()}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or processing ticket");
            return Result.Failure<Guid>("Creation encountered an error. Please try again.");
        }
    }
    public async Task<TicketEditContext?> GetEditContextAsync(Guid id, ClaimsPrincipal user)
    {
        return await _ticketContextFacade.GetEditContextAsync(id, user);
    }
    public async Task<TicketEditContext> GetEditReloadContextAsync(Guid id, ClaimsPrincipal user)
    {
        return await _ticketContextFacade.GetEditReloadContextAsync(id, user);
    }

    public async Task<Result> UpdateTicketAsync(Guid id, EditTicketViewModel viewModel, IFormCollection form, ClaimsPrincipal user)
    {
        var ticketToUpdate = await _ticketReadService.GetTicketForEditAsync(id);
        if (ticketToUpdate == null)
            return Result.Failure("Ticket not found");

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        try
        {
            // Validate authorization using domain logic (Phase 3)
            ticketToUpdate.ValidateCanEdit(userId, userRoles);

            // Track original status for transition validation
            var originalStatus = ticketToUpdate.TicketStatus;

            // Validate status transition BEFORE modifying entity
            // This must be done first because ValidateCanChangeStatus checks current TicketStatus
            if (originalStatus != viewModel.TicketStatus)
            {
                ticketToUpdate.ValidateCanChangeStatus(userId, userRoles, viewModel.TicketStatus);
            }

            // Now safe to update properties
            ticketToUpdate.Description = viewModel.Description;
            ticketToUpdate.TicketStatus = viewModel.TicketStatus;
            ticketToUpdate.CompletionTarget = viewModel.CompletionTarget;
            ticketToUpdate.CustomerId = viewModel.CustomerId;
            ticketToUpdate.ProjectGuid = viewModel.ProjectGuid;

            var domainId = ticketToUpdate.DomainId ?? _domainConfig.GetDefaultDomainId();
            var formDictionary = form.ToDictionary(x => x.Key, x => x.Value.ToString());
            ticketToUpdate.CustomFieldsJson = _ticketReadService.ParseCustomFields(domainId, formDictionary);

            // Validate required fields
            ticketToUpdate.ValidateRequiredFieldsOrThrow();

            var success = await _ticketWorkflowService.UpdateTicketAsync(ticketToUpdate);
            if (success)
            {
                return Result.Success();
            }
            else
            {
                return Result.Failure("Failed to update ticket. Please try again.");
            }
        }
        catch (TicketMasala.Domain.Exceptions.DomainRuleException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating ticket {TicketId}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed updating ticket {TicketId}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket {TicketId}", id);
            return Result.Failure("An unexpected error occurred.");
        }
    }
}
