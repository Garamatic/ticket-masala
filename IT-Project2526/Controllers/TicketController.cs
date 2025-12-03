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
        private readonly IGerdaService _gerdaService;
        private readonly ITicketService _ticketService;
        private readonly ILogger<TicketController> _logger;

        public TicketController(
            IGerdaService gerdaService,
            ITicketService ticketService,
            ILogger<TicketController> logger)
        {
            _gerdaService = gerdaService;
            _ticketService = ticketService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var tickets = await _ticketService.GetAllTicketsAsync();
                return View(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tickets");
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
                            var agent = await _ticketService.GetEmployeeByIdAsync(topRecommendation.AgentId);
                            if (agent != null)
                            {
                                // Calculate current workload using service
                                var currentWorkload = await _ticketService.GetEmployeeCurrentWorkloadAsync(agent.Id);
                                
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

            var agent = await _ticketService.GetEmployeeByIdAsync(agentId);
            TempData["Success"] = $"Ticket successfully assigned to {agent?.FirstName} {agent?.LastName}!";
            return RedirectToAction(nameof(Detail), new { id = ticketGuid });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var ticket = await _ticketService.GetTicketForEditAsync(id.Value);

            if (ticket == null) return NotFound();

            // Get all users for the dropdown
            var responsibleUsers = await _ticketService.GetAllUsersSelectListAsync();

            // Map the database data to the ViewModel
            var viewModel = new EditTicketViewModel
            {
                Guid = ticket.Guid,
                Description = ticket.Description,
                TicketStatus = ticket.TicketStatus,
                CompletionTarget = ticket.CompletionTarget,
                ResponsibleUserId = ticket.Responsible?.Id, // ID of current responsible

                // Fill the dropdown list
                ResponsibleUsers = responsibleUsers
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
                var ticketToUpdate = await _ticketService.GetTicketForEditAsync(id);
                if (ticketToUpdate == null) return NotFound();

                // Update properties based on the ViewModel
                ticketToUpdate.Description = viewModel.Description;
                ticketToUpdate.TicketStatus = viewModel.TicketStatus;
                ticketToUpdate.CompletionTarget = viewModel.CompletionTarget;

                try
                {
                    var success = await _ticketService.UpdateTicketAsync(ticketToUpdate);
                    if (success)
                    {
                        return RedirectToAction(nameof(Detail), new { id = ticketToUpdate.Guid });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to update ticket. Please try again.");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
            }

            // If validation fails, reload the dropdowns and show the view again
            viewModel.ResponsibleUsers = await _ticketService.GetAllUsersSelectListAsync();
            return View(viewModel);
        }
    }
}
