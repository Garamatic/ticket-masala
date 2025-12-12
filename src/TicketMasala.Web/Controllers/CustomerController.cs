using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Web.Utilities;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.ViewModels.Customers;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = $"{Constants.RoleAdmin},{Constants.RoleEmployee}")]
public class CustomerController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly MasalaDbContext _context;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(
        IUserRepository userRepository,
        MasalaDbContext context,
        ILogger<CustomerController> logger)
    {
        _userRepository = userRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var customers = await _userRepository.GetAllCustomersAsync();

            // Get project counts for each customer
            var customerIds = customers.Select(c => c.Id).ToList();
            var projectCounts = await _context.Projects
                .Where(p => p.Customer != null && customerIds.Contains(p.Customer.Id))
                .GroupBy(p => p.Customer!.Id)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CustomerId, x => x.Count);

            var viewModels = customers.Select(c => new CustomerListViewModel
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email ?? string.Empty,
                ProjectCount = projectCounts.TryGetValue(c.Id, out var count) ? count : 0
            }).ToList();

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

        // Get customer's projects  
        var projects = await _context.Projects
            .Include(p => p.ProjectManager)
            .Where(p => p.Customer != null && p.Customer.Id == id)
            .ToListAsync();

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
}
