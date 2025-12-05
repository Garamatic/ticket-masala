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

        // Get a random project (optional, but good for realism)
        var project = await _context.Projects
            .Include(p => p.Customers)
            .Where(p => p.Customers.Any(c => c.Id == randomCustomer.Id))
            .OrderBy(r => Guid.NewGuid()) // Random sort
            .FirstOrDefaultAsync(cancellationToken);

        if (project == null)
        {
             // Fallback to any project
             project = await _context.Projects.OrderBy(r => Guid.NewGuid()).FirstOrDefaultAsync(cancellationToken);
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
