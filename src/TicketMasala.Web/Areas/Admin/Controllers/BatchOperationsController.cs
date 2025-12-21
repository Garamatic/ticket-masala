using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Data;

namespace TicketMasala.Web.Areas.Admin.Controllers;

/// <summary>
/// Batch Operations Controller - Monitor and manage batch jobs.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BatchOperationsController : Controller
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<BatchOperationsController> _logger;

    public BatchOperationsController(
        MasalaDbContext context,
        ILogger<BatchOperationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);

        var viewModel = new BatchOperationsViewModel
        {
            TicketsAssignedToday = await _context.Tickets
                .CountAsync(t => t.ResponsibleId != null && t.CreationDate.Date == today),
            TicketsAssignedThisWeek = await _context.Tickets
                .CountAsync(t => t.ResponsibleId != null && t.CreationDate.Date >= weekAgo),
            TicketsClosedToday = await _context.Tickets
                .CountAsync(t => t.CompletionDate.HasValue && t.CompletionDate.Value.Date == today),
            TicketsClosedThisWeek = await _context.Tickets
                .CountAsync(t => t.CompletionDate.HasValue && t.CompletionDate.Value.Date >= weekAgo),
            UnassignedTickets = await _context.Tickets
                .CountAsync(t => t.ResponsibleId == null && t.Status != "Closed" && t.Status != "Done"),
            RecentBatchActivity = await GetRecentBatchActivityAsync()
        };

        return View(viewModel);
    }

    private async Task<List<BatchActivityItem>> GetRecentBatchActivityAsync()
    {
        var recentAssignments = await _context.Tickets
            .Where(t => t.ResponsibleId != null && t.CreationDate > DateTime.UtcNow.AddDays(-7))
            .GroupBy(t => t.CreationDate.Date)
            .Select(g => new BatchActivityItem
            {
                Date = g.Key,
                AssignedCount = g.Count(),
                Description = $"Assigned {g.Count()} tickets"
            })
            .OrderByDescending(x => x.Date)
            .Take(10)
            .ToListAsync();

        return recentAssignments;
    }
}

public class BatchOperationsViewModel
{
    public int TicketsAssignedToday { get; set; }
    public int TicketsAssignedThisWeek { get; set; }
    public int TicketsClosedToday { get; set; }
    public int TicketsClosedThisWeek { get; set; }
    public int UnassignedTickets { get; set; }
    public List<BatchActivityItem> RecentBatchActivity { get; set; } = new();
}

public class BatchActivityItem
{
    public DateTime Date { get; set; }
    public int AssignedCount { get; set; }
    public string Description { get; set; } = "";
}
