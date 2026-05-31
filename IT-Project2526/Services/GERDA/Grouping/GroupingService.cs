using IT_Project2526.Services.GERDA.Models;
using IT_Project2526.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IT_Project2526.Services.GERDA.Grouping;

/// <summary>
/// G - Grouping: Spam detection and ticket clustering implementation
/// </summary>
public class GroupingService : IGroupingService
{
    private readonly ITProjectDB _context;
    private readonly GerdaConfig _config;
    private readonly ILogger<GroupingService> _logger;

    public GroupingService(
        ITProjectDB context,
        GerdaConfig config,
        ILogger<GroupingService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && _config.GerdaAI.SpamDetection.IsEnabled;

    public async Task<Guid?> CheckAndGroupTicketAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Grouping service is disabled");
            return null;
        }

        var ticket = await _context.Tickets
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Guid == ticketGuid);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found", ticketGuid);
            return null;
        }

        // Get recent tickets from the same customer
        var recentTicketGuids = await GetGroupableTicketsAsync(
            ticket.CustomerId ?? string.Empty,
            _config.GerdaAI.SpamDetection.TimeWindowMinutes);

        // Remove the current ticket from the list
        recentTicketGuids.Remove(ticketGuid);

        // Check if we exceed the threshold
        if (recentTicketGuids.Count < _config.GerdaAI.SpamDetection.MaxTicketsPerUser)
        {
            _logger.LogDebug("Ticket {TicketGuid} does not meet grouping threshold", ticketGuid);
            return null;
        }

        _logger.LogInformation(
            "GERDA-G: Detected {Count} tickets from same customer in {Window} minutes - grouping ticket {TicketGuid}",
            recentTicketGuids.Count + 1,
            _config.GerdaAI.SpamDetection.TimeWindowMinutes,
            ticketGuid);

        // Find or create parent ticket
        var parentTicketGuid = await FindOrCreateParentTicketAsync(ticket, recentTicketGuids);
        
        if (parentTicketGuid.HasValue && _config.GerdaAI.SpamDetection.Action == "AutoMerge")
        {
            // Link this ticket to the parent
            ticket.ParentTicketGuid = parentTicketGuid;
            
            // Add GERDA tag
            ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags) 
                ? "Spam-Cluster" 
                : $"{ticket.GerdaTags},Spam-Cluster";
                
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "GERDA-G: Linked ticket {TicketGuid} to parent ticket {ParentGuid}",
                ticketGuid, parentTicketGuid);
        }

        return parentTicketGuid;
    }

    public async Task<List<Guid>> GetGroupableTicketsAsync(string customerId, int timeWindowMinutes)
    {
        if (string.IsNullOrEmpty(customerId))
            return new List<Guid>();

        var cutoffTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);

        var ticketGuids = await _context.Tickets
            .Where(t => t.CustomerId == customerId)
            .Where(t => t.CreationDate >= cutoffTime)
            .Where(t => t.ParentTicketGuid == null) // Not already grouped
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .OrderByDescending(t => t.CreationDate)
            .Select(t => t.Guid)
            .ToListAsync();

        return ticketGuids;
    }

    private async Task<Guid?> FindOrCreateParentTicketAsync(Ticket ticket, List<Guid> relatedTicketGuids)
    {
        // Check if any of the related tickets already has a parent
        var existingParent = await _context.Tickets
            .Where(t => relatedTicketGuids.Contains(t.Guid))
            .Where(t => t.ParentTicketGuid != null)
            .Select(t => t.ParentTicketGuid)
            .FirstOrDefaultAsync();

        if (existingParent.HasValue)
        {
            return existingParent;
        }

        // Check if any ticket is already a parent
        var ticketAsParent = await _context.Tickets
            .Where(t => relatedTicketGuids.Contains(t.Guid))
            .Where(t => _context.Tickets.Any(child => child.ParentTicketGuid == t.Guid))
            .Select(t => t.Guid)
            .FirstOrDefaultAsync();

        if (ticketAsParent != Guid.Empty)
        {
            return ticketAsParent;
        }

        // For now, use the first ticket as the parent instead of creating new
        // This avoids issues with required Customer field
        var firstTicketGuid = relatedTicketGuids.FirstOrDefault();
        if (firstTicketGuid != Guid.Empty)
        {
            var firstTicket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == firstTicketGuid);
            if (firstTicket != null)
            {
                var prefix = _config.GerdaAI.SpamDetection.GroupedTicketPrefix;
                firstTicket.Description = $"{prefix}{firstTicket.Description}";
                firstTicket.GerdaTags = "Parent-Cluster";
                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "GERDA-G: Promoted ticket {TicketGuid} as parent for {Count} grouped tickets",
                    firstTicketGuid, relatedTicketGuids.Count);
                    
                return firstTicketGuid;
            }
        }

        return null;
    }
}
