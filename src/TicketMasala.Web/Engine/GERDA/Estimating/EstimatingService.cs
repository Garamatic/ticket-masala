using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Engine.GERDA.Estimating;

using TicketMasala.Web.Engine.GERDA.Strategies;
using TicketMasala.Web.Services.Configuration;
using TicketMasala.Web.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// E - Estimating: Complexity estimation using Fibonacci points
/// Uses category-based lookup from configuration.
/// </summary>
public class EstimatingService : IEstimatingService
{
    private readonly ITProjectDB _context;
    private readonly GerdaConfig _config;
    private readonly IStrategyFactory _strategyFactory;
    private readonly IDomainConfigurationService _domainConfigService;
    private readonly ILogger<EstimatingService> _logger;

    public EstimatingService(
        ITProjectDB context,
        GerdaConfig config,
        IStrategyFactory strategyFactory,
        IDomainConfigurationService domainConfigService,
        ILogger<EstimatingService> logger)
    {
        _context = context;
        _config = config;
        _strategyFactory = strategyFactory;
        _domainConfigService = domainConfigService;
        _logger = logger;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && _config.GerdaAI.ComplexityEstimation.IsEnabled;

    public async Task<int> EstimateComplexityAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Estimating service is disabled");
            return _config.GerdaAI.ComplexityEstimation.DefaultEffortPoints;
        }

        var ticket = await _context.Tickets
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Guid == ticketGuid);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found for complexity estimation", ticketGuid);
            return _config.GerdaAI.ComplexityEstimation.DefaultEffortPoints;
        }

        // Determine Domain and Strategy
        var domainId = ticket.DomainId ?? _domainConfigService.GetDefaultDomainId();
        var domainConfig = _domainConfigService.GetDomain(domainId);
        var strategyName = domainConfig?.AiStrategies.Estimating ?? "CategoryLookup";
        
        int effortPoints = _config.GerdaAI.ComplexityEstimation.DefaultEffortPoints;

        try
        {
            var strategy = _strategyFactory.GetStrategy<IEstimatingStrategy, int>(strategyName);
            effortPoints = strategy.EstimateComplexity(ticket, _config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute estimating strategy {StrategyName} for ticket {TicketGuid}", strategyName, ticketGuid);
            // Fallback to default
        }

        // Update the ticket with estimated effort
        ticket.EstimatedEffortPoints = effortPoints;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "GERDA-E: Estimated ticket {TicketGuid} complexity as {Points} points using strategy {StrategyName}",
            ticketGuid, effortPoints, strategyName);

        return effortPoints;
    }

    public int GetComplexityByCategory(string category)
    {
        // For backward compatibility / utility usage, simpler implementation directly accessing config
        // This bypasses strategy but satisfies interface contract
        if (string.IsNullOrEmpty(category))
            return _config.GerdaAI.ComplexityEstimation.DefaultEffortPoints;

        var map = _config.GerdaAI.ComplexityEstimation.CategoryComplexityMap
            .ToDictionary(
                c => c.Category.ToLowerInvariant(),
                c => c.EffortPoints,
                StringComparer.OrdinalIgnoreCase);

        // Try exact match first
        if (map.TryGetValue(category.ToLowerInvariant(), out var points))
            return points;

        // Try partial match
        var partialMatch = map.Keys
            .FirstOrDefault(k => category.ToLowerInvariant().Contains(k) || k.Contains(category.ToLowerInvariant()));

        if (partialMatch != null)
            return map[partialMatch];

        return _config.GerdaAI.ComplexityEstimation.DefaultEffortPoints;
    }

}
