using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using IT_Project2526.Models;
using IT_Project2526;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.ViewModels;

//Database Connectie moet nog toegevoegd worden

namespace IT_Project2526.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ITProjectDB _context;
        public ProjectsController(ITProjectDB context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            //Projecten uit Db halen met hun tickets
            var projectsOfDb = _context.Projects
                                       .Include(p => p.Tasks)
                                       .Include(p => p.ProjectManager)
                                       .ToList();

            //Models naar ViewModels
            List<ProjectTicketViewModel> viewModels = projectsOfDb.Select(p => new ProjectTicketViewModel
            {
                ProjectDetails = new ProjectViewModel
                {
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    ProjectManager = p.ProjectManager,
                },
                Tasks = p.Tasks.Select(t => new TicketViewModel
                {
                    Guid = t.Guid,
                    TicketStatus = t.TicketStatus,
                    CreationDate = t.CreationDate,
                }).ToList()
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        public IActionResult NewProject()
        { //Ophalen bestaande klanten en voorbereiden van de new project form
            var existingCustomers = _context.Customers.ToList();
            var viewModel = new NewProject
            {
                CustomerList = existingCustomers.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList(),
                IsNewCustomer = true
            };

            return View(viewModel); //Geeft de NewProject.cshtml weer
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Dit beschermt tegen cross-site request forgery (CSRF) aanvallen
        public async Task<IActionResult> NewProject(NewProject viewModel)
        {
            // Valideer het ViewModel op basis van de validatie-attributen in de klasse
            if (ModelState.IsValid)
            {
                Customer projectCustomer;

                if (viewModel.IsNewCustomer)
                {
                    // Maak een nieuwe klant aan en vul de gegevens in
                    projectCustomer = new Customer
                    {
                        Name = viewModel.NewCustomerName,
                        Email = viewModel.NewCustomerEmail,
                        Phone = viewModel.NewCustomerPhone,
                        // De Guid en CreationDate worden door de BaseModel gezet
                    };
                    _context.Customers.Add(projectCustomer);
                }
                else
                {
                    // Zoek de bestaande klant op basis van de SelectedCustomerId
                    projectCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == viewModel.SelectedCustomerId);

                    if (projectCustomer == null)
                    {
                        // Als de geselecteerde klant niet bestaat, toon een foutmelding
                        ModelState.AddModelError("SelectedCustomerId", "De geselecteerde klant is ongeldig.");
                        // Herlaad de klantenlijst en retourneer de view om de fout te tonen
                        viewModel.CustomerList = _context.Customers.ToList().Select(c => new SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = c.Name
                        }).ToList();
                        return View(viewModel);
                    }
                }

                // Maak het nieuwe Project aan en vul de gegevens in
                var newProject = new Project
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Status = Status.Pending, // Stel de status in op basis van je logica
                    // ProjectManager moet ook nog worden ingevuld, bijvoorbeeld met de ingelogde gebruiker
                    ProjectManager = null, // Dit moet je nog implementeren
                    // ... andere Project eigenschappen
                };

                // Koppel de klant aan het project (als er een referentie is)
                // Als je een CustomerId in het Project model hebt, vul je deze hier in:
                // newProject.CustomerId = projectCustomer.Guid;

                _context.Projects.Add(newProject);

                // Sla alle wijzigingen op in de database
                await _context.SaveChangesAsync();

                // Na succesvol opslaan, verwijs door naar de projectenlijst
                return RedirectToAction("Index");
            }

            // Als de validatie faalt, herlaad dan de klantenlijst en toon het formulier opnieuw
            viewModel.CustomerList = _context.Customers.ToList().Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
            return View(viewModel);
        }


    }

 
}
