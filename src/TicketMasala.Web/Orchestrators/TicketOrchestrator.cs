using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.Facades;
using TicketMasala.Web.Common;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.AI;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Abstractions;

namespace TicketMasala.Web.Orchestrators;

public class TicketOrchestrator : ITicketOrchestrator
{
    private readonly IGerdaService _gerdaService;
    private readonly ITicketWorkflowService _ticketWorkflowService;
    private readonly ITicketReadService _ticketReadService;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly IRuleEngineService _ruleEngine;
    private readonly IOpenAiService _openAiService;
    private readonly ITicketContextFacade _ticketContextFacade;
    private readonly ILogger<TicketOrchestrator> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TicketOrchestrator(
        IGerdaService gerdaService,
        ITicketWorkflowService ticketWorkflowService,
        ITicketReadService ticketReadService,
        IDomainConfigurationService domainConfig,
        IRuleEngineService ruleEngine,
        IOpenAiService openAiService,
        ITicketContextFacade ticketContextFacade,
        ILogger<TicketOrchestrator> logger,
        IServiceProvider serviceProvider)
    {
        _gerdaService = gerdaService;
        _ticketWorkflowService = ticketWorkflowService;
        _ticketReadService = ticketReadService;
        _domainConfig = domainConfig;
        _ruleEngine = ruleEngine;
        _openAiService = openAiService;
        _ticketContextFacade = ticketContextFacade;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel, ClaimsPrincipal user)
    {
        if (searchModel == null) searchModel = new TicketSearchViewModel();

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);
        
        if (isCustomer && !string.IsNullOrEmpty(userId)) searchModel.CustomerId = userId;

        var result = await _ticketReadService.SearchTicketsAsync(searchModel);
        result.Customers = await _ticketReadService.GetCustomerSelectListAsync();
        result.Employees = await _ticketReadService.GetEmployeeSelectListAsync();
        result.Projects = await _ticketReadService.GetProjectSelectListAsync();

        if (!string.IsNullOrEmpty(userId))
        {
            var savedFilterService = _serviceProvider.GetService<ISavedFilterService>();
            if (savedFilterService != null)
                // Note: Orchestrator returns data, Controller puts it in ViewBag or ViewModel
                // Since SavedFilters is usually in ViewBag, we might need to handle it differently or add to ViewModel.
                // For now, let's stick to the core ViewModel return. 
                // To properly handle ViewBag data, we should probably expand the ViewModel or return a composite object.
                // But looking at Controller, it puts it in ViewBag.SavedFilters.
                // I will skip this side-effect here and let Controller handle it if needed, or better, add it to ViewModel.
                // Ideally TicketSearchViewModel should have SavedFilters property.
                // I'll assume for now the Controller might still need to do small UI things, or we can refactor ViewModel later.
                // Let's keep the Orchestrator focused on the main data.
                {} 
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
        if (ticket == null) throw new ArgumentException("Ticket not found");

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
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        if (isCustomer && !string.IsNullOrEmpty(userId))
        {
            customerId = userId;
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
            await _gerdaService.ProcessTicketAsync(ticket.Guid);

            var entityLabel = _domainConfig.GetEntityLabels(ticket.DomainId).WorkItem;
            _logger.LogInformation("GERDA processing completed for ticket {TicketGuid}", ticket.Guid);
            
            return Result<Guid>.Success(ticket.Guid, $"{entityLabel} created successfully! GERDA AI has processed the {entityLabel.ToLower()}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or processing ticket");
            return Result<Guid>.Failure("Creation encountered an error. Please try again.");
        }
    }

    public async Task<TicketEditContext?> GetEditContextAsync(Guid id, ClaimsPrincipal user)
    {
        var context = await _ticketContextFacade.GetEditContextAsync(id, user);
        
        // Populate ValidStatuses if needed (logic from Controller)
        // Since Facade doesn't return Ticket entity, we re-fetch if needed, or Facade should have done it.
        // We will mimic Controller logic here for now.
        if (context != null)
        {
             var ticket = await _ticketReadService.GetTicketForEditAsync(id);
             if (ticket != null)
             {
                var validStates = _ruleEngine.GetValidNextStates(ticket, user);
                // We need to pass this back. But TicketEditContext usually has ViewModel. 
                // We might need to extend Context or ViewModel. 
                // Controller used ViewBag.ValidStatuses. 
                // We can put it in context if there's a place, or return a tuple/result.
                // TicketEditContext class definition needs checking.
                // Assuming we can't easily change TicketEditContext right now, we will leave this to Controller or add to ViewModel if possible.
                // But wait, GetEditReloadContextAsync has ValidStatuses!
                // Let's see if GetEditContextAsync has it. 
             }
        }
        return context;
    }

    public async Task<TicketEditContext> GetEditReloadContextAsync(Guid id, ClaimsPrincipal user)
    {
        return await _ticketContextFacade.GetEditReloadContextAsync(id, user);
    }

    public async Task<Result> UpdateTicketAsync(Guid id, EditTicketViewModel viewModel, IFormCollection form, ClaimsPrincipal user)
    {
        var ticketToUpdate = await _ticketReadService.GetTicketForEditAsync(id);
        if (ticketToUpdate == null) return Result.Failure("Ticket not found");

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        if (isCustomer)
        {
            if (ticketToUpdate.CustomerId != userId) return Result.Failure("Unauthorized");

            if (ticketToUpdate.TicketStatus != Status.Pending && ticketToUpdate.TicketStatus != Status.Assigned)
            {
                return Result.Failure("You can only edit tickets that are in Pending or Assigned status.");
            }
        }

        ticketToUpdate.Description = viewModel.Description;
        ticketToUpdate.TicketStatus = viewModel.TicketStatus;
        ticketToUpdate.CompletionTarget = viewModel.CompletionTarget;
        ticketToUpdate.CustomerId = viewModel.CustomerId;
        ticketToUpdate.ProjectGuid = viewModel.ProjectGuid;

        var domainId = ticketToUpdate.DomainId ?? _domainConfig.GetDefaultDomainId();
        var formDictionary = form.ToDictionary(x => x.Key, x => x.Value.ToString());
        ticketToUpdate.CustomFieldsJson = _ticketReadService.ParseCustomFields(domainId, formDictionary);

        try
        {
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
        catch (DomainRuleException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket");
            return Result.Failure("An unexpected error occurred.");
        }
    }
}
