using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Models;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TicketMasala.Web.Utilities;

namespace TicketMasala.Web.Engine.GERDA.Grouping;

/// <summary>
/// G - Grouping: Spam detection and ticket clustering implementation
/// </summary>
public class GroupingService : IGroupingService
{
    private readonly MasalaDbContext _context;
    private readonly GerdaConfig _config;
    private readonly ILogger<GroupingService> _logger;

    public GroupingService(
        MasalaDbContext context,
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

        // V2: Use ContentHash for instant duplicate detection
        // Fallback: If ContentHash is null (legacy data), compute/save it now
        if (string.IsNullOrEmpty(ticket.ContentHash))
        {
            ticket.ContentHash = TicketHasher.ComputeContentHash(ticket.Description, ticket.CustomerId ?? "");
            await _context.SaveChangesAsync();
        }

        // Find GROUPABLE tickets with SAME HASH within WINDOW
        // We look for existing tickets that are NOT the current one
        var timeWindowMinutes = _config.GerdaAI.SpamDetection.TimeWindowMinutes;
        var cutoffTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);

        var duplicateCandidates = await _context.Tickets
            .Where(t => t.ContentHash == ticket.ContentHash)
            .Where(t => t.CreationDate >= cutoffTime)
            .Where(t => t.Guid != ticketGuid) // Exclude self
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .OrderByDescending(t => t.CreationDate)
            .Select(t => t.Guid)
            .ToListAsync();

        if (duplicateCandidates.Count == 0)
        {
            return null;
        }

        _logger.LogInformation(
            "GERDA-G: Detected {Count} duplicate tickets (Hash: {Hash}) - grouping ticket {TicketGuid}",
            duplicateCandidates.Count,
            ticket.ContentHash,
            ticketGuid);

        // Find or create parent ticket from these candidates
        var parentTicketGuid = await FindOrCreateParentTicketAsync(ticket, duplicateCandidates);

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

    // Maintained for interface compatibility but refactored to check Hash if simpler listing needed
    public async Task<List<Guid>> GetGroupableTicketsAsync(string customerId, int timeWindowMinutes)
    {
        if (string.IsNullOrEmpty(customerId))
            return new List<Guid>();

        var cutoffTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);

        // This method was "Get by customer", but v2 prefers Hash match.
        // We will keep it as "Get by customer" for legacy calls, or update callers?
        // Callers: Only CheckAndGroupTicketAsync calls it internally in previous version.
        // If interface requires it, we leave it as customer lookup, but CheckAndGroup uses Hash logic above.

        var ticketGuids = await _context.Tickets
            .Where(t => t.CustomerId == customerId)
            .Where(t => t.CreationDate >= cutoffTime)
            .Where(t => t.ParentTicketGuid == null)
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

        // Promote the OLDEST ticket in the group (or the first found) as parent
        var bestParentGuid = relatedTicketGuids.LastOrDefault(); // Oldest typically
        if (bestParentGuid != Guid.Empty)
        {
            var parentTicket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == bestParentGuid);
            if (parentTicket != null)
            {
                var prefix = _config.GerdaAI.SpamDetection.GroupedTicketPrefix;
                if (!parentTicket.Description.StartsWith(prefix))
                {
                    parentTicket.Description = $"{prefix}{parentTicket.Description}";
                    parentTicket.GerdaTags = "Parent-Cluster";
                }
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "GERDA-G: Promoted ticket {TicketGuid} as parent for group",
                    bestParentGuid);

                return bestParentGuid;
            }
        }

        return null;
    }
}
