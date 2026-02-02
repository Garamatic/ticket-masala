using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Facades;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service for ticket detail view operations.
/// Handles fetching ticket details, recommendations, and knowledge suggestions.
/// Single responsibility: Detail view concerns only.
/// </summary>
public interface ITicketDetailService
{
    Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketId, string? userId, bool isCustomer);
    Task<TicketDetailContext> GetDetailContextAsync(TicketDetailsViewModel viewModel);
}

public class TicketDetailService : ITicketDetailService
{
    private readonly ITicketReadService _ticketReadService;
    private readonly IDispatchingService _dispatchingService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly ILogger<TicketDetailService> _logger;

    public TicketDetailService(
        ITicketReadService ticketReadService,
        IDispatchingService dispatchingService,
        IKnowledgeService knowledgeService,
        ILogger<TicketDetailService> logger)
    {
        _ticketReadService = ticketReadService;
        _dispatchingService = dispatchingService;
        _knowledgeService = knowledgeService;
        _logger = logger;
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
        if (string.IsNullOrWhiteSpace(viewModel.ResponsibleId))
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

        return viewModel;
    }

    public Task<TicketDetailContext> GetDetailContextAsync(TicketDetailsViewModel viewModel)
    {
        // Domain configuration is now handled by the caller (TicketContextFacade)
        // This method is kept for potential future use
        return Task.FromResult(new TicketDetailContext
        {
            DomainId = viewModel.DomainId,
            WorkItemTypeCode = viewModel.WorkItemTypeCode
        });
    }
}
