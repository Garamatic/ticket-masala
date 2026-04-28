using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.Utilities;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service for ticket detail view operations.
/// Handles fetching ticket details, recommendations, and knowledge suggestions.
/// Single responsibility: Detail view concerns only.
/// </summary>
public interface ITicketDetailService
{
    Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketId, string? userId, bool isCustomer);
    Task<Facades.TicketDetailContext> GetDetailContextAsync(TicketDetailsViewModel viewModel);
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
        var viewModel = await _ticketReadService.GetTicketDetailsAsync(ticketId).ConfigureAwait(false);
        if (viewModel == null)
            return null;

        // Note: Authorization is primarily handled at the module/controller level using
        // ClaimsPrincipal with full role information. This service-level check only handles
        // the simple customer ownership case for defense in depth.
        if (isCustomer && viewModel.CustomerId != userId)
        {
            return null; // Return null to indicate "not found or not accessible" (don't leak existence)
        }

        // Get recommended agent for unassigned tickets
        if (string.IsNullOrWhiteSpace(viewModel.ResponsibleId))
        {
            try
            {
                var recommendations = await _dispatchingService.GetTopRecommendedAgentsAsync(ticketId, 1).ConfigureAwait(false);
                if (recommendations != null && recommendations.Any())
                {
                    var topRecommendation = recommendations.First();
                    var agent = await _ticketReadService.GetEmployeeByIdAsync(topRecommendation.AgentId).ConfigureAwait(false);
                    if (agent != null)
                    {
                        var currentWorkload = await _ticketReadService.GetEmployeeCurrentWorkloadAsync(agent.Id).ConfigureAwait(false);
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
            var ticket = await _ticketReadService.GetTicketForEditAsync(ticketId).ConfigureAwait(false);
            if (ticket != null)
            {
                var suggestions = await _knowledgeService.GetSuggestedArticlesAsync(ticket).ConfigureAwait(false);
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

    public Task<Facades.TicketDetailContext> GetDetailContextAsync(TicketDetailsViewModel viewModel)
    {
        // Domain configuration is now handled by the caller (TicketContextFacade)
        // This method is kept for potential future use
        return Task.FromResult(new Facades.TicketDetailContext
        {
            DomainId = viewModel.DomainId ?? string.Empty,
            WorkItemTypeCode = viewModel.WorkItemTypeCode
        });
    }
}
