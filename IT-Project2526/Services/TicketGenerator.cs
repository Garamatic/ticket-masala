using IT_Project2526.Models;
using IT_Project2526.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IT_Project2526;

namespace IT_Project2526.Services;

public class TicketGenerator : ITicketGenerator
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITProjectDB _context;
    private readonly ILogger<TicketGenerator> _logger;

    public TicketGenerator(
        ITicketService ticketService,
        UserManager<ApplicationUser> userManager,
        ITProjectDB context,
        ILogger<TicketGenerator> logger)
    {
        _ticketService = ticketService;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task GenerateRandomTicketAsync(CancellationToken cancellationToken = default)
    {
        // Get a random customer
        var customer = await _userManager.GetUsersInRoleAsync(Constants.RoleCustomer);
        if (customer.Count == 0) return;
        
        var randomCustomer = customer[new Random().Next(customer.Count)];

        // Get a random project safely
        Project? project = null;

        // Strategy 1: Try to find a project for this customer
        var customerProjectIds = await _context.Projects
            .Where(p => p.Customers.Any(c => c.Id == randomCustomer.Id))
            .Select(p => p.Guid)
            .ToListAsync(cancellationToken);

        if (customerProjectIds.Any())
        {
            var randomProjectId = customerProjectIds[new Random().Next(customerProjectIds.Count)];
            project = await _context.Projects
                .Include(p => p.ProjectManager)
                .FirstOrDefaultAsync(p => p.Guid == randomProjectId, cancellationToken);
        }

        // Strategy 2: Fallback to any project
        if (project == null)
        {
            var allProjectIds = await _context.Projects.Select(p => p.Guid).ToListAsync(cancellationToken);
            if (allProjectIds.Any())
            {
                 var randomProjectId = allProjectIds[new Random().Next(allProjectIds.Count)];
                 project = await _context.Projects
                    .Include(p => p.ProjectManager)
                    .FirstOrDefaultAsync(p => p.Guid == randomProjectId, cancellationToken);
            }
        }

        if (project == null) return; // No projects exist

        var title = RandomDataHelper.GenerateTicketTitle();
        var description = RandomDataHelper.GenerateTicketDescription();
        
        // Create ticket using the service method which handles defaults and notifications
        var ticket = await _ticketService.CreateTicketAsync(
            description: $"{title} - {description}",
            customerId: randomCustomer.Id,
            responsibleId: null, // Let GERDA or manual assignment handle this
            projectGuid: project.Guid,
            completionTarget: DateTime.UtcNow.AddDays(new Random().Next(1, 14))
        );

        // The service method sets defaults. If we want random priority, we might need to update it after creation.
        ticket.PriorityScore = new Random().NextDouble() * 100;
        
        await _ticketService.UpdateTicketAsync(ticket);

        _logger.LogInformation("Generated random ticket: {Title} for Customer: {Customer}", title, randomCustomer.UserName);
    }
}
