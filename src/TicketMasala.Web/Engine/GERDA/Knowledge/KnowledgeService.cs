using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Strategies;

namespace TicketMasala.Web.Engine.GERDA.Knowledge;

/// <summary>
/// K - Knowledge: GERDA Knowledge Recommendation Service
/// </summary>
public class KnowledgeService : IKnowledgeService
{
    private readonly MasalaDbContext _context;
    private readonly GerdaConfig _config;
    private readonly IStrategyFactory _strategyFactory;
    private readonly IDomainConfigurationService _domainConfigService;
    private readonly ILogger<KnowledgeService> _logger;

    public KnowledgeService(
        MasalaDbContext context,
        GerdaConfig config,
        IStrategyFactory strategyFactory,
        IDomainConfigurationService domainConfigService,
        ILogger<KnowledgeService> _logger)
    {
        _context = context;
        _config = config;
        _strategyFactory = strategyFactory;
        _domainConfigService = domainConfigService;
        this._logger = _logger;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && (_config.GerdaAI.Knowledge?.IsEnabled ?? false);

    public async Task<List<KnowledgeSuggestion>> GetSuggestedArticlesAsync(Ticket ticket, int maxSuggestions = 3)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Knowledge service is disabled");
            return new List<KnowledgeSuggestion>();
        }

        // Determine Domain and Strategy
        var domainId = ticket.DomainId ?? _domainConfigService.GetDefaultDomainId();
        var domainConfig = _domainConfigService.GetDomain(domainId);

        // We might want to add Knowledge strategy to DomainConfig later, but for now use similarity or a default from config
        var strategyName = _config.GerdaAI.Knowledge?.StrategyName ?? "Similarity";

        try
        {
            // Fetch all verified KB articles for analysis
            // In a real large-scale system, we'd use a vector DB or search index
            var articles = await _context.KnowledgeBaseArticles
                .Where(a => a.IsVerified)
                .ToListAsync();

            var strategy = _strategyFactory.GetStrategy<IKnowledgeStrategy, List<KnowledgeSuggestion>>(strategyName);
            var suggestions = await strategy.FindRelatedArticlesAsync(ticket, articles, maxSuggestions);

            _logger.LogInformation("GERDA-K: Found {Count} suggested articles for ticket {TicketGuid} using {Strategy}",
                suggestions.Count, ticket.Guid, strategyName);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GERDA-K: Failed to get knowledge suggestions for ticket {TicketGuid}", ticket.Guid);
            return new List<KnowledgeSuggestion>();
        }
    }
}
