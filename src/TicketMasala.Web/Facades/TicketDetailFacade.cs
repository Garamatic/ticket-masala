using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Domain.Configuration;
using TicketMasala.Web.Engine.GERDA.Configuration;
using System.Text.Json;

namespace TicketMasala.Web.Facades;

public class TicketDetailFacade : ITicketDetailFacade
{
    private readonly ITicketReadService _ticketReadService;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly IDispatchingService? _dispatchingService;
    private readonly IKnowledgeService? _knowledgeService;
    private readonly ILogger<TicketDetailFacade> _logger;

    public TicketDetailFacade(
        ITicketReadService ticketReadService,
        IDomainConfigurationService domainConfig,
        ILogger<TicketDetailFacade> logger,
        IEnumerable<IDispatchingService> dispatchingServices,
        IEnumerable<IKnowledgeService> knowledgeServices)
    {
        _ticketReadService = ticketReadService;
        _domainConfig = domainConfig;
        _logger = logger;
        _dispatchingService = dispatchingServices.FirstOrDefault();
        _knowledgeService = knowledgeServices.FirstOrDefault();
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

        return Task.FromResult(context).Result;
    }
}
