using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.ViewModels.Portal;
using TicketMasala.Web.ViewModels.Tickets;
using System.Security.Claims;



namespace TicketMasala.Web.Controllers;

[Authorize(Roles = Constants.RoleCustomer)]
public class PortalController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly IProjectService _projectService;
    private readonly IGerdaService _gerdaService;
    private readonly ILogger<PortalController> _logger;

    public PortalController(
        ITicketService ticketService,
        IProjectService projectService,
        IGerdaService gerdaService,
        ILogger<PortalController> logger)
    {
        _ticketService = ticketService;
        _projectService = projectService;
        _gerdaService = gerdaService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Show recent tickets for this customer
        var searchModel = new TicketSearchViewModel
        {
            CustomerId = userId,
            Page = 1,
            PageSize = 10
        };

        var result = await _ticketService.SearchTicketsAsync(searchModel);

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> CreateTicket()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var model = new InternalCreateTicketViewModel
        {
            Projects = await GetCustomerProjectsSelectList(userId!)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket(InternalCreateTicketViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!ModelState.IsValid)
        {
            model.Projects = await GetCustomerProjectsSelectList(userId!);
            return View(model);
        }

        try
        {
            // Create ticket - Customer is implicitly the Creator and CustomerId
            var fullDescription = $"subject: {model.Title}\n\n{model.Description}";

            var ticket = await _ticketService.CreateTicketAsync(
                fullDescription,
                userId!,
                responsibleId: null,
                projectGuid: model.ProjectGuid,
                completionTarget: null
            );

            // Trigger GERDA processing
            _logger.LogInformation("Processing customer portal ticket {TicketGuid} with GERDA AI", ticket.Guid);
            await _gerdaService.ProcessTicketAsync(ticket.Guid);

            TempData["Success"] = "Ticket created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket from portal");
            ModelState.AddModelError("", "An error occurred while creating the ticket. Please try again.");
            model.Projects = await GetCustomerProjectsSelectList(userId!);
            return View(model);
        }
    }

    private async Task<IEnumerable<SelectListItem>> GetCustomerProjectsSelectList(string userId)
    {
        return await _ticketService.GetProjectSelectListAsync();
    }
}
