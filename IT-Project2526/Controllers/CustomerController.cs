using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.ViewModels;
using IT_Project2526.Utilities;

namespace IT_Project2526.Controllers
{
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public class CustomerController : Controller
    {
        private readonly ITProjectDB _context;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ITProjectDB context, ILogger<CustomerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Index: Toon de lijst van klanten
        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModels = await _context.Customers
                                               .AsNoTracking()
                                               .Select(c => new CustomerListViewModel
                                               {
                                                   Id = c.Id,
                                                   FirstName = c.FirstName,
                                                   LastName = c.LastName,
                                                   Email = c.Email ?? string.Empty,
                                                   ProjectCount = c.Projects.Count()
                                               })
                                               .ToListAsync();

                return View(viewModels);
            }
            catch (System.Exception ex)
            {
                var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Error loading customer list. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500);
            }
        }

        // Details: Toon de details van één klant
        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var viewModel = await _context.Customers
                                              .AsNoTracking()
                                              .Include(c => c.Projects)
                                                  .ThenInclude(p => p.ProjectManager)
                                              .Include(c => c.Projects)
                                                  .ThenInclude(p => p.Tasks) // Include tasks to count them
                                              .Where(c => c.Id == id)
                                              .Select(customer => new CustomerDetailViewModel
                                              {
                                                  Id = customer.Id,
                                                  Name = customer.Name,
                                                  Email = customer.Email ?? string.Empty,
                                                  Phone = customer.Phone ?? string.Empty,
                                                  Code = customer.Code ?? string.Empty,
                                                  Projects = customer.Projects
                                                                    .Select(p => new ProjectViewModel
                                                                    {
                                                                        Guid = p.Guid,
                                                                        Name = p.Name,
                                                                        Description = p.Description,
                                                                        Status = p.Status,
                                                                        ProjectManagerName = p.ProjectManager != null 
                                                                            ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}"
                                                                            : "Unassigned",
                                                                        ProjectManager = p.ProjectManager,
                                                                        TicketCount = p.Tasks.Count
                                                                    })
                                                                    .ToList()
                                              })
                                              .FirstOrDefaultAsync();

                if (viewModel == null)
                {
                    return NotFound();
                }

                return View(viewModel);
            }
            catch (System.Exception ex)
            {
                var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Error loading customer details for id {CustomerId}. CorrelationId: {CorrelationId}", id, correlationId);
                return StatusCode(500);
            }
        }
    }
}

