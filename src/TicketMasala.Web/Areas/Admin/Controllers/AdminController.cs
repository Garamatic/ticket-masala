using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Data;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin Dashboard Controller - Provides system overview and quick stats.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly MasalaDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        MasalaDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;

        var viewModel = new AdminDashboardViewModel
        {
            TotalUsers = await _userManager.Users.CountAsync(),
            ActiveUsers = await _userManager.Users.CountAsync(u => u.LockoutEnd == null || u.LockoutEnd < DateTime.UtcNow),
            TotalTickets = await _context.Tickets.CountAsync(),
            TicketsToday = await _context.Tickets.CountAsync(t => t.CreationDate.Date == today),
            TicketsResolvedToday = await _context.Tickets.CountAsync(t => t.CompletionDate.HasValue && t.CompletionDate.Value.Date == today),
            OpenTickets = await _context.Tickets.CountAsync(t => t.Status != "Closed" && t.Status != "Resolved" && t.Status != "Done"),
            RecentActivity = await GetRecentActivityAsync()
        };

        return View(viewModel);
    }

    private async Task<List<ActivityItem>> GetRecentActivityAsync()
    {
        var recentTickets = await _context.Tickets
            .OrderByDescending(t => t.CreationDate)
            .Take(10)
            .Select(t => new ActivityItem
            {
                Type = "Ticket",
                Description = t.Title,
                Timestamp = t.CreationDate,
                User = t.ResponsibleId ?? "Unassigned"
            })
            .ToListAsync();

        return recentTickets;
    }
}

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalTickets { get; set; }
    public int TicketsToday { get; set; }
    public int TicketsResolvedToday { get; set; }
    public int OpenTickets { get; set; }
    public List<ActivityItem> RecentActivity { get; set; } = new();
}

public class ActivityItem
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string User { get; set; } = "";
}
