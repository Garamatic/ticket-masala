using TicketMasala.Dispatch.Common.Models;
using TicketMasala.Web.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Engine.Common;

/// <summary>
/// Repository adapter that implements IWorkItemRepository for TicketMasala Tickets.
/// Bridges TicketMasala's EF Core data layer to the generic Dispatch.Common algorithms.
/// </summary>
public class TicketWorkItemRepository : IWorkItemRepository
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<TicketWorkItemRepository> _logger;

    public TicketWorkItemRepository(MasalaDbContext context, ILogger<TicketWorkItemRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Get a single ticket by ID, return as IWorkItem</summary>
    public async Task<IWorkItem?> GetByIdAsync(string workItemId)
    {
        if (!Guid.TryParse(workItemId, out var guid))
        {
            _logger.LogWarning("Invalid work item ID format: {Id}", workItemId);
            return null;
        }

        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == guid);
        if (ticket == null) return null;

        return new TicketWorkItemAdapter(ticket);
    }

    /// <summary>Get all open tickets (not completed/cancelled)</summary>
    public async Task<IEnumerable<IWorkItem>> GetOpenItemsAsync()
    {
        // Open = not completed, not cancelled
        var openStatuses = new[] { "New", "Triaged", "InProgress", "OnHold" };
        
        var tickets = await _context.Tickets
            .Where(t => openStatuses.Contains(t.Status))
            .ToListAsync();

        return tickets.Select(t => new TicketWorkItemAdapter(t) as IWorkItem).ToList();
    }

    /// <summary>Get tickets filtered by work type</summary>
    public async Task<IEnumerable<IWorkItem>> GetByWorkTypeAsync(string workType)
    {
        var tickets = await _context.Tickets
            .Where(t => t.TicketType != null && t.TicketType.ToString() == workType)
            .ToListAsync();

        return tickets.Select(t => new TicketWorkItemAdapter(t) as IWorkItem).ToList();
    }

    /// <summary>Get multiple work items by IDs in a single batch query</summary>
    public async Task<IEnumerable<IWorkItem>> GetBatchAsync(IEnumerable<string> workItemIds)
    {
        var guids = new List<Guid>();
        foreach (var id in workItemIds)
        {
            if (Guid.TryParse(id, out var guid))
                guids.Add(guid);
        }

        if (!guids.Any()) return Enumerable.Empty<IWorkItem>();

        var tickets = await _context.Tickets
            .Where(t => guids.Contains(t.Guid))
            .ToListAsync();

        return tickets.Select(t => new TicketWorkItemAdapter(t) as IWorkItem).ToList();
    }

    /// <summary>Alias for GetBatchAsync - get multiple work items by IDs</summary>
    public async Task<IEnumerable<IWorkItem>> GetByIdsAsync(IEnumerable<string> ids)
    {
        return await GetBatchAsync(ids);
    }
}
