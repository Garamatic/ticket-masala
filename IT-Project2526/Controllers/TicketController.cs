using IT_Project2526;
using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using IT_Project2526.Services;
using IT_Project2526.Services.GERDA;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Controllers
{
    [Authorize] // All authenticated users can access tickets
    public class TicketController : Controller
    {
        private readonly ITProjectDB _context;
        private readonly IGerdaService _gerdaService;
        private readonly ITicketService _ticketService;
        private readonly ILogger<TicketController> _logger;

        public TicketController(
            ITProjectDB context,
            IGerdaService gerdaService,
            ITicketService ticketService,
            ILogger<TicketController> logger)
        {
            _context = context;
            _gerdaService = gerdaService;
            _ticketService = ticketService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var tickets = await _context.Tickets
                    .AsNoTracking()
                    .Include(t => t.Customer)
                    .Include(t => t.Responsible)
                    .Select(t => new TicketViewModel
                    {
                        Guid = t.Guid,
                        Description = t.Description,
                        TicketStatus = t.TicketStatus,
                        CreationDate = t.CreationDate,
                        CompletionTarget = t.CompletionTarget,
                        ResponsibleName = t.Responsible != null
                            ? $"{t.Responsible.FirstName} {t.Responsible.LastName}"
                            : "Not Assigned",
                        CustomerName = t.Customer != null
                            ? $"{t.Customer.FirstName} {t.Customer.LastName}"
                            : "Unknown"
                    })
                    .ToListAsync();

                return View(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _ticketService.GetCustomerSelectListAsync();
            ViewBag.Employees = await _ticketService.GetEmployeeSelectListAsync();
            ViewBag.Projects = await _ticketService.GetProjectSelectListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string description, string customerId, string? responsibleId, Guid? projectGuid, DateTime? completionTarget)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                ModelState.AddModelError("description", "Description is required");
            }

            if (string.IsNullOrWhiteSpace(customerId))
            {
                ModelState.AddModelError("customerId", "Customer is required");
            }

            if (!ModelState.IsValid)
            {
                // Reload dropdowns
                ViewBag.Customers = await _ticketService.GetCustomerSelectListAsync();
                ViewBag.Employees = await _ticketService.GetEmployeeSelectListAsync();
                ViewBag.Projects = await _ticketService.GetProjectSelectListAsync();

                return View();
            }

            try
            {
                // Create ticket via service
                var ticket = await _ticketService.CreateTicketAsync(description, customerId, responsibleId, projectGuid, completionTarget);

                // Process with GERDA AI
                _logger.LogInformation("Processing ticket {TicketGuid} with GERDA AI", ticket.Guid);
                await _gerdaService.ProcessTicketAsync(ticket.Guid);
                
                TempData["Success"] = "Ticket created successfully! GERDA AI has processed the ticket (estimated effort, priority, and tags assigned).";
                _logger.LogInformation("GERDA processing completed for ticket {TicketGuid}", ticket.Guid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating or processing ticket");
                TempData["Warning"] = "Ticket creation encountered an error. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detail(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var viewModel = await _ticketService.GetTicketDetailsAsync(id.Value);
            
            if (viewModel == null)
            { 
                return NotFound(); 
            }

            // Get recommended agent from Dispatching service (if ticket is unassigned)
            if (string.IsNullOrWhiteSpace(viewModel.ResponsibleId))
            {
                try
                {
                    var dispatchingService = HttpContext.RequestServices.GetService<Services.GERDA.Dispatching.IDispatchingService>();
                    if (dispatchingService != null)
                    {
                        var recommendations = await dispatchingService.GetTopRecommendedAgentsAsync(id.Value, 1);
                        if (recommendations != null && recommendations.Any())
                        {
                            var topRecommendation = recommendations.First();
                            var agent = await _context.Employees.FindAsync(topRecommendation.AgentId);
                            if (agent != null)
                            {
                                // Calculate current workload
                                var currentWorkload = await _context.Tickets
                                    .Where(t => t.ResponsibleId == agent.Id && 
                                               (t.TicketStatus == Status.Assigned || t.TicketStatus == Status.InProgress))
                                    .SumAsync(t => t.EstimatedEffortPoints);
                                
                                viewModel.RecommendedAgent = new RecommendedAgentInfo
                                {
                                    AgentId = agent.Id,
                                    AgentName = $"{agent.FirstName} {agent.LastName}",
                                    AffinityScore = topRecommendation.Score,
                                    CurrentWorkload = currentWorkload,
                                    MaxCapacity = agent.MaxCapacityPoints
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get recommended agent for ticket {TicketGuid}", id.Value);
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToRecommended(Guid ticketGuid, string agentId)
        {
            var success = await _ticketService.AssignTicketAsync(ticketGuid, agentId);
            
            if (!success)
            {
                TempData["Error"] = "Failed to assign ticket. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            var agent = await _context.Employees.FindAsync(agentId);
            TempData["Success"] = $"Ticket successfully assigned to {agent?.FirstName} {agent?.LastName}!";
            return RedirectToAction(nameof(Detail), new { id = ticketGuid });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                                        .Include(t => t.Responsible)
                                        .FirstOrDefaultAsync(t => t.Guid == id);

            if (ticket == null) return NotFound();

            // Haal alle mogelijke verantwoordelijke gebruikers op voor de dropdown
            var responsibleUsers = await _context.Users.ToListAsync();

            // Map de databasegegevens naar het ViewModel
            var viewModel = new EditTicketViewModel
            {
                Guid = ticket.Guid,
                Description = ticket.Description,
                TicketStatus = ticket.TicketStatus,
                CompletionTarget = ticket.CompletionTarget,
                ResponsibleUserId = ticket.Responsible?.Id, // ID van de huidige verantwoordelijke

                // Vul de dropdown lijst
                ResponsibleUsers = responsibleUsers.Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = $"{u.FirstName} {u.LastName}"
                }).ToList()
            };

            return View(viewModel);
            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditTicketViewModel viewModel)
        {
            if (id != viewModel.Guid) return NotFound();

            if (ModelState.IsValid)
            {
                var ticketToUpdate = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == id);
                if (ticketToUpdate == null) return NotFound();

                // Werk de eigenschappen bij op basis van het ViewModel
                ticketToUpdate.Description = viewModel.Description;
                ticketToUpdate.TicketStatus = viewModel.TicketStatus;
                ticketToUpdate.CompletionTarget = viewModel.CompletionTarget;
                // Update de verantwoordelijke (u moet nog logica hebben om ApplicationUser te vinden op basis van de Guid/Id)

                try
                {
                    _context.Update(ticketToUpdate);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Detail), new { id = ticketToUpdate.Guid }); 
                }
                catch (DbUpdateConcurrencyException)
                {
                    
                    throw;
                }
            }

            // Als validatie faalt, herlaad de dropdowns en toon de view opnieuw
            viewModel.ResponsibleUsers = await _context.Users.Select(u => new SelectListItem { Value = u.Id.ToString(), Text = $"{u.FirstName} {u.LastName}" }).ToListAsync();
            return View(viewModel);
        }
    }
}
