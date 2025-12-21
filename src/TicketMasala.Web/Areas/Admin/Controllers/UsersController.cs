using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Data;
using TicketMasala.Web.Models;

namespace TicketMasala.Web.Areas.Admin.Controllers;

/// <summary>
/// User Management Controller - View and manage application users.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var viewModel = new List<UserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            viewModel.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                FullName = user.FullName ?? "",
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow,
                Roles = roles.ToList()
            });
        }

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            _logger.LogInformation("User {UserId} unlocked by admin", id);
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            _logger.LogInformation("User {UserId} locked by admin", id);
        }

        return RedirectToAction(nameof(Index));
    }
}

public class UserViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string UserName { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool IsLocked { get; set; }
    public List<string> Roles { get; set; } = new();
}
