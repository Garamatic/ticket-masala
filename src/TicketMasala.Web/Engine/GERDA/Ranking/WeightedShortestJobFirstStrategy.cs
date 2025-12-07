using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Models;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.Compiler;
using System.Security.Claims;

namespace TicketMasala.Web.Engine.GERDA.Ranking;

public class WeightedShortestJobFirstStrategy : IJobRankingStrategy
{
    public string Name => "WSJF";

    private readonly IDomainConfigurationService _domainConfigService;
    private readonly RuleCompilerService _ruleCompiler;
    private readonly ILogger<WeightedShortestJobFirstStrategy> _logger;

    public WeightedShortestJobFirstStrategy(
        IDomainConfigurationService domainConfigurationService,
        RuleCompilerService ruleCompiler,
        ILogger<WeightedShortestJobFirstStrategy> logger)
    {
        _domainConfigService = domainConfigurationService;
        _ruleCompiler = ruleCompiler;
        _logger = logger;
    }

    public double CalculateScore(Ticket ticket, GerdaConfig config)
    {
        // Calculate Cost of Delay (urgency)
        var costOfDelay = CalculateCostOfDelay(ticket, config);

        // Get Job Size (effort points from Estimating service)
        var jobSize = ticket.EstimatedEffortPoints > 0 ? ticket.EstimatedEffortPoints : 5; // Default to medium

        // WSJF Formula: Priority = Cost of Delay / Job Size
        return costOfDelay / (double)jobSize;
    }

    private double CalculateCostOfDelay(Ticket ticket, GerdaConfig config)
    {
        var domainConfig = _domainConfigService.GetDomain(ticket.DomainId ?? "IT");
        var rankingConfig = domainConfig?.AiStrategies?.Ranking;

        // Base Urgency: Age-based (Older = More Urgent)
        var now = DateTime.UtcNow;
        var age = (now - ticket.CreationDate).TotalDays;
        
        // Use configured SLA weight as base multiplier, or default 1.0
        double baseWeight = config.GerdaAI.Ranking.SlaWeight > 0 ? config.GerdaAI.Ranking.SlaWeight : 1.0;
        
        // Initial Score based on Age
        double urgencyScore = (age * baseWeight / 10.0);

        // If ticket has specific deadline, add base urgency for nearing it
        if (ticket.CompletionTarget.HasValue)
        {
             var daysUntil = (ticket.CompletionTarget.Value - now).TotalDays;
             if (daysUntil < 0) urgencyScore += 100.0; // Overdue base penalty
             else if (daysUntil < 10) urgencyScore += (10.0 - daysUntil); 
        }

        // Apply Configured Multipliers (Dynamic Rules)
        if (rankingConfig != null && rankingConfig.Multipliers != null)
        {
            var domainId = ticket.DomainId ?? "IT";
            
            for (int i = 0; i < rankingConfig.Multipliers.Count; i++)
            {
                var multiplier = rankingConfig.Multipliers[i];
                var ruleKey = $"ranking:{domainId}:{i}";
                
                // Get pre-compiled delegate
                var ruleFunc = _ruleCompiler.GetRuleDelegate(ruleKey);
                
                // Evaluate (User principal is null here as this is system background/calculation)
                if (ruleFunc(ticket, null!)) 
                {
                    urgencyScore *= multiplier.Value;
                    // _logger.LogDebug("Applied multiplier {Value} for rule {RuleIndex}", multiplier.Value, i);
                }
            }
        }
        
        // Ensure non-zero
        return Math.Max(urgencyScore, 1.0);
    }
}
