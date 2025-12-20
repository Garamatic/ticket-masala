using TicketMasala.Web.Models;
using TicketMasala.Web.ViewModels.ApplicationUsers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TicketMasala.Web.Utilities;

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ApplicationUsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<ApplicationUsersController> _logger;

    public ApplicationUsersController(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager,
        ILogger<ApplicationUsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? "",
                    Phone = user.Phone,
                    Roles = string.Join(", ", roles),
                    Type = user is Employee ? "Employee" : "Customer"
                });
            }

            return View(userViewModels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load users for Index");
            return Problem("An unexpected error occurred while loading users.");
        }
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View(new UserCreateViewModel { Role = Constants.RoleCustomer, Email = "", FirstName = "", LastName = "", Password = "", ConfirmPassword = "" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        ApplicationUser user;

        // Determine Entity Type based on Role
        if (model.Role == Constants.RoleEmployee || model.Role == Constants.RoleAdmin)
        {
            user = new Employee
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                Phone = model.Phone,
                Team = model.Team ?? "Unassigned",
                Level = model.Level ?? EmployeeType.Support,
                Language = model.Language,
                MaxCapacityPoints = model.MaxCapacityPoints
            };
        }
        else
        {
            user = new ApplicationUser // Customer alias not available here
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                Phone = model.Phone
            };
        }

        try
        {
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user {Email}", model.Email);
            ModelState.AddModelError("", "An unexpected error occurred while creating the user.");
        }

        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View(model);
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        
        var model = new UserEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? "",
            Phone = user.Phone,
            Role = roles.FirstOrDefault() ?? "Customer",
            UserName = user.UserName // UserName is optional in ViewModel now? No, I added it as string?
        };

        if (user is Employee emp)
        {
            model.Team = emp.Team;
            model.Level = emp.Level;
            model.Language = emp.Language;
            model.MaxCapacityPoints = emp.MaxCapacityPoints;
        }

        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Phone = model.Phone;
        user.Email = model.Email;
        user.UserName = model.Email;

        if (user is Employee emp)
        {
            emp.Team = model.Team ?? emp.Team;
            emp.Level = model.Level ?? emp.Level;
            emp.Language = model.Language;
            emp.MaxCapacityPoints = model.MaxCapacityPoints;
        }

        try
        {
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update Roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {Id}", model.Id);
            ModelState.AddModelError("", "An unexpected error occurred.");
        }

        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View(model);
    }
}
