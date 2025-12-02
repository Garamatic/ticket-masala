using IT_Project2526.Services.GERDA.Models;
using IT_Project2526.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Services.GERDA.Ranking;

/// <summary>
/// R - Ranking: WSJF (Weighted Shortest Job First) priority calculation
/// Calculates priority score: Cost of Delay / Job Size
/// </summary>
public class RankingService : IRankingService
{
    private readonly ITProjectDB _context;
    private readonly GerdaConfig _config;
    private readonly ILogger<RankingService> _logger;

    public RankingService(
        ITProjectDB context,
        GerdaConfig config,
        ILogger<RankingService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && _config.GerdaAI.Ranking.IsEnabled;

    public async Task<double> CalculatePriorityScoreAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Ranking service is disabled");
            return 0.0;
        }

        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == ticketGuid);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found for ranking", ticketGuid);
            return 0.0;
        }

        // Calculate Cost of Delay (urgency based on SLA and age)
        var costOfDelay = CalculateCostOfDelay(ticket);

        // Get Job Size (effort points from Estimating service)
        var jobSize = ticket.EstimatedEffortPoints > 0 ? ticket.EstimatedEffortPoints : 5; // Default to medium

        // WSJF Formula: Priority = Cost of Delay / Job Size
        var priorityScore = costOfDelay / (double)jobSize;

        // Update the ticket
        ticket.PriorityScore = priorityScore;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "GERDA-R: Ticket {TicketGuid} ranked with priority score {Score:F2} (CoD: {CoD:F2}, Size: {Size})",
            ticketGuid, priorityScore, costOfDelay, jobSize);

        return priorityScore;
    }

    public async Task RecalculateAllPrioritiesAsync()
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Ranking service is disabled, skipping recalculation");
            return;
        }

        _logger.LogInformation("GERDA-R: Starting priority recalculation for all open tickets");

        var openTickets = await _context.Tickets
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .ToListAsync();

        foreach (var ticket in openTickets)
        {
            try
            {
                await CalculatePriorityScoreAsync(ticket.Guid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GERDA-R: Failed to recalculate priority for ticket {TicketGuid}", ticket.Guid);
            }
        }

        _logger.LogInformation("GERDA-R: Completed priority recalculation for {Count} tickets", openTickets.Count);
    }

    public async Task<List<Guid>> GetPrioritizedTicketGuidsAsync(int? projectId = null)
    {
        var query = _context.Tickets
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed);

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        var prioritizedGuids = await query
            .OrderByDescending(t => t.PriorityScore)
            .ThenBy(t => t.CreationDate) // Tie-breaker: older tickets first
            .Select(t => t.Guid)
            .ToListAsync();

        return prioritizedGuids;
    }

    /// <summary>
    /// Calculate Cost of Delay based on SLA breach risk and ticket age
    /// </summary>
    private double CalculateCostOfDelay(Ticket ticket)
    {
        var now = DateTime.UtcNow;
        var age = (now - ticket.CreationDate).TotalDays;

        // If ticket has a completion target (SLA), calculate urgency based on that
        if (ticket.CompletionTarget.HasValue)
        {
            var daysUntilDeadline = (ticket.CompletionTarget.Value - now).TotalDays;

            if (daysUntilDeadline <= 0)
            {
                // Already breached SLA - CRITICAL
                return _config.GerdaAI.Ranking.SlaWeight * 10.0;
            }
            else if (daysUntilDeadline <= 1)
            {
                // Less than 1 day until breach - URGENT
                return _config.GerdaAI.Ranking.SlaWeight * 5.0;
            }
            else if (daysUntilDeadline <= 3)
            {
                // Less than 3 days until breach - HIGH
                return _config.GerdaAI.Ranking.SlaWeight * 2.0;
            }
            else
            {
                // Normal urgency based on time remaining
                // More time remaining = lower urgency
                return _config.GerdaAI.Ranking.SlaWeight / daysUntilDeadline;
            }
        }

        // Fallback: use ticket age as urgency factor
        // Older tickets get higher urgency
        return age * _config.GerdaAI.Ranking.SlaWeight / 10.0;
    }
}
