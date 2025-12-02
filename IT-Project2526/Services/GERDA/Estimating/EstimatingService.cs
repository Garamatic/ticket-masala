using IT_Project2526.Services.GERDA.Models;
using IT_Project2526.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Services.GERDA.Estimating;

/// <summary>
/// E - Estimating: Complexity estimation using Fibonacci points
/// Uses category-based lookup from configuration.
/// </summary>
public class EstimatingService : IEstimatingService
{
    private readonly ITProjectDB _context;
    private readonly GerdaConfig _config;
    private readonly ILogger<EstimatingService> _logger;
    private readonly Dictionary<string, int> _complexityMap;

    public EstimatingService(
        ITProjectDB context,
        GerdaConfig config,
        ILogger<EstimatingService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
        
        // Build the complexity lookup dictionary
        _complexityMap = _config.GerdaAI.ComplexityEstimation.CategoryComplexityMap
            .ToDictionary(
                c => c.Category.ToLowerInvariant(),
                c => c.EffortPoints,
                StringComparer.OrdinalIgnoreCase);
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

        // Try to determine category from ticket description or project
        var category = ExtractCategory(ticket);
        var effortPoints = GetComplexityByCategory(category);

        // Update the ticket with estimated effort
        ticket.EstimatedEffortPoints = effortPoints;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "GERDA-E: Estimated ticket {TicketGuid} complexity as {Points} points (category: {Category})",
            ticketGuid, effortPoints, category);

        return effortPoints;
    }

    public int GetComplexityByCategory(string category)
    {
        if (string.IsNullOrEmpty(category))
            return _config.GerdaAI.ComplexityEstimation.DefaultEffortPoints;

        // Try exact match first
        if (_complexityMap.TryGetValue(category.ToLowerInvariant(), out var points))
            return points;

        // Try partial match
        var partialMatch = _complexityMap.Keys
            .FirstOrDefault(k => category.ToLowerInvariant().Contains(k) || k.Contains(category.ToLowerInvariant()));

        if (partialMatch != null)
            return _complexityMap[partialMatch];

        return _config.GerdaAI.ComplexityEstimation.DefaultEffortPoints;
    }

    /// <summary>
    /// Extract category from ticket description or project name
    /// This is a simple keyword-based approach - could be enhanced with ML.NET text classification
    /// </summary>
    private string ExtractCategory(Ticket ticket)
    {
        var text = $"{ticket.Description} {ticket.Project?.Name}".ToLowerInvariant();

        // Check for known categories in the text
        foreach (var category in _complexityMap.Keys)
        {
            if (text.Contains(category.ToLowerInvariant()))
                return category;
        }

        // Simple keyword matching for common IT categories
        if (text.Contains("password") || text.Contains("wachtwoord") || text.Contains("login"))
            return "Password Reset";
        
        if (text.Contains("hardware") || text.Contains("laptop") || text.Contains("computer") || text.Contains("printer"))
            return "Hardware Request";
        
        if (text.Contains("bug") || text.Contains("error") || text.Contains("fout") || text.Contains("crash"))
            return "Software Bug";
        
        if (text.Contains("outage") || text.Contains("down") || text.Contains("offline") || text.Contains("storing"))
            return "System Outage";

        return "Other";
    }
}
