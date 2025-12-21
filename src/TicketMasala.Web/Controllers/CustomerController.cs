using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.ViewModels.Customers;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = $"{Constants.RoleAdmin},{Constants.RoleEmployee}")]
public class CustomerController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        ILogger<CustomerController> logger)
    {
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            IEnumerable<ApplicationUser> users;
            if (User.IsInRole(Constants.RoleAdmin))
            {
                users = await _userRepository.GetAllUsersAsync();
            }
            else
            {
                users = await _userRepository.GetAllCustomersAsync();
            }

            var viewModels = new List<CustomerListViewModel>();
            foreach (var user in users)
            {
                var projects = (await _projectRepository.GetByCustomerIdAsync(user.Id)).ToList();
                
                string role = "Customer";
                if (user is Employee employee)
                {
                    role = employee.Level.ToString();
                }

                viewModels.Add(new CustomerListViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    ProjectCount = projects.Count,
                    Role = role
                });
            }
            return View(viewModels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers");
            return StatusCode(500);
        }
    }

    public async Task<IActionResult> Detail(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var customer = await _userRepository.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return NotFound();
        }


        var projects = await _projectRepository.GetByCustomerIdAsync(id);

        var viewModel = new CustomerDetailViewModel
        {
            Id = customer.Id,
            Name = $"{customer.FirstName} {customer.LastName}",
            Email = customer.Email ?? string.Empty,
            Projects = projects.Select(p => new TicketMasala.Web.ViewModels.Projects.ProjectViewModel
            {
                Guid = p.Guid,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                ProjectManager = p.ProjectManager!,
                ProjectManagerName = p.ProjectManager != null
                    ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}"
                    : "Unassigned"
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var customer = await _userRepository.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        var viewModel = new CustomerEditViewModel
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email ?? string.Empty,
            PhoneNumber = customer.PhoneNumber ?? string.Empty
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CustomerEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var customer = await _userRepository.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        customer.FirstName = viewModel.FirstName;
        customer.LastName = viewModel.LastName;
        customer.Email = viewModel.Email;
        customer.PhoneNumber = viewModel.PhoneNumber;

        var success = await _userRepository.UpdateCustomerAsync(customer);
        if (success)
        {
            TempData["Success"] = "Customer updated successfully.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        ModelState.AddModelError("", "Failed to update customer. Please try again.");
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var success = await _userRepository.DeleteCustomerAsync(id);
        if (success)
        {
            TempData["Success"] = "Customer deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = "Failed to delete customer. The customer may have associated projects or tickets.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}
