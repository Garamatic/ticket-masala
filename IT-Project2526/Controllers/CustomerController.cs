using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using IT_Project2526;

namespace IT_Project2526.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ITProjectDB _context;

        public CustomerController(ITProjectDB context)
        {
            _context = context;
        }

        // Index: Toon de lijst van klanten
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers.Include(c => c.Projects).ToListAsync();

            var viewModels = customers.Select(c => new CustomerListViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                ProjectCount = c.Projects?.Count ?? 0 // Aantal projecten tellen
            }).ToList();

            return View(viewModels);
        }

        // Details: Toon de details van één klant
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _context.Customers
                                         .Include(c => c.Projects)
                                         .ThenInclude(p => p.ProjectManager) // Include de manager van het project
                                         .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            var viewModel = new CustomerDetailViewModel
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone, // Telefoonnummer zit in de Identity basisklasse
                Code = customer.Code,
                Projects = customer.Projects.Select(p => new ProjectViewModel
                {
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    ProjectManager = p.ProjectManager
                }).ToList()
            };

            return View(viewModel);
        }
    }
}

