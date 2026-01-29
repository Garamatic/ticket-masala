using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Domain.Configuration;
using TicketMasala.Web.Engine.GERDA.Configuration;
using System.Text.Json;
using TicketMasala.Web.Engine.Projects;
using Microsoft.AspNetCore.Mvc.Rendering;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.Compiler;

namespace TicketMasala.Web.Facades;

public class TicketContextFacade : ITicketContextFacade
{
    private readonly ITicketReadService _ticketReadService;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly IDispatchingService? _dispatchingService;
    private readonly IKnowledgeService? _knowledgeService;
    private readonly IProjectReadService _projectReadService;
    private readonly IRuleEngineService _ruleEngine;
    private readonly ILogger<TicketContextFacade> _logger;

    public TicketContextFacade(
        ITicketReadService ticketReadService,
        IDomainConfigurationService domainConfig,
        ILogger<TicketContextFacade> logger,
        IEnumerable<IDispatchingService> dispatchingServices,
        IEnumerable<IKnowledgeService> knowledgeServices,
        IProjectReadService projectReadService,
        IRuleEngineService ruleEngine)
    {
        _ticketReadService = ticketReadService;
        _domainConfig = domainConfig;
        _logger = logger;
        _dispatchingService = dispatchingServices.FirstOrDefault();
        _knowledgeService = knowledgeServices.FirstOrDefault();
        _projectReadService = projectReadService;
        _ruleEngine = ruleEngine;
    }

    public async Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketId, string? userId, bool isCustomer)
    {
        var viewModel = await _ticketReadService.GetTicketDetailsAsync(ticketId);
        if (viewModel == null) return null;

        if (isCustomer && viewModel.CustomerId != userId)
        {
            throw new UnauthorizedAccessException("Customer is not authorized to view this ticket.");
        }

        // Get recommended agent for unassigned tickets
        if (string.IsNullOrWhiteSpace(viewModel.ResponsibleId) && _dispatchingService != null)
        {
            try
            {
                var recommendations = await _dispatchingService.GetTopRecommendedAgentsAsync(ticketId, 1);
                if (recommendations != null && recommendations.Any())
                {
                    var topRecommendation = recommendations.First();
                    var agent = await _ticketReadService.GetEmployeeByIdAsync(topRecommendation.AgentId);
                    if (agent != null)
                    {
                        var currentWorkload = await _ticketReadService.GetEmployeeCurrentWorkloadAsync(agent.Id);
                        viewModel.RecommendedAgent = new RecommendedAgentInfo
                        {
                            AgentId = agent.Id,
                            AgentName = $"{agent.FirstName} {agent.LastName}",
                            AffinityScore = topRecommendation.Score,
                            CurrentWorkload = currentWorkload,
                            MaxCapacity = agent.MaxCapacityPoints
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get recommended agent for ticket {TicketGuid}", ticketId);
            }
        }

        // Get suggested KB articles (GERDA-K)
        if (_knowledgeService != null)
        {
            try
            {
                var ticket = await _ticketReadService.GetTicketForEditAsync(ticketId);
                if (ticket != null)
                {
                    var suggestions = await _knowledgeService.GetSuggestedArticlesAsync(ticket);
                    viewModel.SuggestedArticles = suggestions.Select(s => new KnowledgeSuggestionInfo
                    {
                        ArticleId = s.Article.Id,
                        Title = s.Article.Title,
                        RelevanceScore = s.RelevanceScore,
                        MatchingReason = s.MatchingReason
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get suggested knowledge for ticket {TicketGuid}", ticketId);
            }
        }

        return viewModel;
    }

    public async Task<TicketDetailContext> GetTicketDetailContextAsync(TicketDetailsViewModel viewModel)
    {
        var domainId = viewModel.DomainId ?? _domainConfig.GetDefaultDomainId();
        
        var context = new TicketDetailContext
        {
            DomainId = domainId,
            EntityLabels = _domainConfig.GetEntityLabels(domainId),
            CustomFields = _domainConfig.GetCustomFields(domainId).ToList(),
            WorkItemTypeCode = viewModel.WorkItemTypeCode
        };

        if (!string.IsNullOrEmpty(viewModel.CustomFieldsJson))
        {
            try 
            { 
                context.CustomFieldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(viewModel.CustomFieldsJson) 
                    ?? new Dictionary<string, object>(); 
            }
            catch 
            { 
                context.CustomFieldValues = new Dictionary<string, object>(); 
            }
        }

        return context;
    }

    public async Task<TicketCreateContext> GetCreateContextAsync(bool isCustomer, string? preselectedCustomerId = null, Guid? projectGuid = null)
    {
        var context = new TicketCreateContext
        {
            IsCustomer = isCustomer,
            Employees = await _ticketReadService.GetEmployeeSelectListAsync(),
            Projects = await _ticketReadService.GetProjectSelectListAsync()
        };

        if (projectGuid.HasValue)
        {
            var project = await _projectReadService.GetProjectDetailsAsync(projectGuid.Value);
            if (project != null && project.ProjectDetails != null)
            {
                context.PreselectedProjectId = project.ProjectDetails.Guid;
                if (!string.IsNullOrEmpty(project.ProjectDetails.CustomerId))
                {
                    preselectedCustomerId = project.ProjectDetails.CustomerId;
                }
            }
        }

        if (!isCustomer)
        {
            context.Customers = await _ticketReadService.GetCustomerSelectListAsync();
            context.PreselectedCustomerId = preselectedCustomerId;
        }
        else
        {
            context.PreselectedCustomerId = preselectedCustomerId; // In caller, this would be current user ID
        }

        var defaultDomain = _domainConfig.GetDefaultDomainId();
        context.DomainId = defaultDomain;
        context.EntityLabels = _domainConfig.GetEntityLabels(defaultDomain);
        context.WorkItemTypes = _domainConfig.GetWorkItemTypes(defaultDomain).ToList();
        context.CustomFields = _domainConfig.GetCustomFields(defaultDomain).ToList();

        return context;
    }

    public async Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, string? userId, bool isCustomer)
    {
        var ticket = await _ticketReadService.GetTicketForEditAsync(ticketId);
        if (ticket == null) return null;

        if (isCustomer)
        {
            if (ticket.CustomerId != userId) 
                throw new UnauthorizedAccessException("Customer is not authorized to edit this ticket.");

            if (ticket.TicketStatus != Status.Pending && ticket.TicketStatus != Status.Assigned)
            {
                // We'll throw a specific exception or handle it in controller, 
                // but facade should probably return null or throw to indicate invalid state.
                // Let's assume controller handles redirection logic, but we enforce the rule here.
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

        var domainId = ticket.DomainId ?? _domainConfig.GetDefaultDomainId();
        
        var context = new TicketEditContext
        {
            ViewModel = viewModel,
            DomainId = domainId,
            EntityLabels = _domainConfig.GetEntityLabels(domainId),
            CustomFields = _domainConfig.GetCustomFields(domainId).ToList(),
            WorkItemTypeCode = ticket.WorkItemTypeCode
        };

        if (!string.IsNullOrEmpty(ticket.CustomFieldsJson))
        {
            try { context.CustomFieldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(ticket.CustomFieldsJson) ?? new Dictionary<string, object>(); }
            catch { context.CustomFieldValues = new Dictionary<string, object>(); }
        }

        // Valid statuses logic needs the User ClaimsPrincipal, but we only passed userId/isCustomer.
        // Ideally we pass the user principal to the facade method if needed, or inject IHttpContextAccessor (discouraged in facades).
        // For now, we will leave ValidStatuses to be populated by the controller or passed in.
        // Actually, let's omit ValidStatuses here and let the controller handle it, 
        // OR pass the user principal.
        
        return context;
    }

    public async Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, System.Security.Claims.ClaimsPrincipal user)
    {
        var context = new TicketEditContext();
        
        var reloadTicket = await _ticketReadService.GetTicketForEditAsync(ticketId);
        if (reloadTicket != null)
        {
            var validStates = _ruleEngine.GetValidNextStates(reloadTicket, user);
            var allowedStatuses = validStates.Union(new[] { reloadTicket.TicketStatus }).Distinct().ToList();
            context.ValidStatuses = new SelectList(allowedStatuses);
        }

        var reloadDomainId = _domainConfig.GetDefaultDomainId();
        context.DomainId = reloadDomainId;
        context.EntityLabels = _domainConfig.GetEntityLabels(reloadDomainId);
        context.CustomFields = _domainConfig.GetCustomFields(reloadDomainId).ToList();
        
        return context;
    }
}
