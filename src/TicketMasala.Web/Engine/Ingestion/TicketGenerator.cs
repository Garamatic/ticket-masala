using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web;
using TicketMasala.Web.Utilities;
using System.Text.Json;

namespace TicketMasala.Web.Engine.Ingestion;

public class TicketGenerator : ITicketGenerator
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MasalaDbContext _context;
    private readonly ILogger<TicketGenerator> _logger;

    public TicketGenerator(
        ITicketService ticketService,
        UserManager<ApplicationUser> userManager,
        MasalaDbContext context,
        ILogger<TicketGenerator> logger)
    {
        _ticketService = ticketService;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task GenerateGoldenPathDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Golden Path Data (Agents, Customers, History)...");

        // 1. Ensure Agents Exist
        var agentDb = await EnsureAgentAsync("agent.db@ticketmasala.com", "Agent DB", "Database Team", "en", "London", new List<string> { "Database", "SQL", "Performance" });
        var agentNet = await EnsureAgentAsync("agent.net@ticketmasala.com", "Agent Network", "Network Team", "fr", "Paris", new List<string> { "Network", "Wifi", "VPN" });

        // 2. Ensure Customers Exist
        var customerSql = await EnsureCustomerAsync("customer.sql@client.com", "Customer SQL", "en");
        var customerWifi = await EnsureCustomerAsync("customer.wifi@client.com", "Customer WiFi", "fr");

        // 3. Generate History (Training Data)
        // 10 tickets: Customer SQL -> Agent DB
        await GenerateHistoryAsync(customerSql, agentDb, "SQL Query Optimization", "Database query is running slow", 10, cancellationToken);
        
        // 10 tickets: Customer WiFi -> Agent Net
        await GenerateHistoryAsync(customerWifi, agentNet, "Wifi Connection Issue", "Cannot connect to office wifi", 10, cancellationToken);

        // 4. Generate Pending Tickets (Demo Data)
        await GeneratePendingTicketAsync(customerSql, "Production DB Slow", "SQL Server high CPU usage", cancellationToken);
        await GeneratePendingTicketAsync(customerWifi, "VPN Disconnected", "Cannot access internal network via VPN", cancellationToken);

        _logger.LogInformation("Golden Path Data Generation Completed.");
    }

    private async Task<ApplicationUser> EnsureAgentAsync(string email, string name, string team, string lang, string region, List<string> skills)
    {
        var agent = await _userManager.FindByEmailAsync(email);
        if (agent == null)
        {
            agent = new Employee
            {
                UserName = email,
                Email = email,
                FirstName = name.Split(' ')[0],
                LastName = name.Substring(name.IndexOf(' ') + 1),
                EmailConfirmed = true,
                Team = team,
                Language = lang,
                Region = region,
                Specializations = JsonSerializer.Serialize(skills),
                MaxCapacityPoints = 20
            };
            await _userManager.CreateAsync(agent, "Password123!");
            await _userManager.AddToRoleAsync(agent, Constants.RoleEmployee);
        }
        else if (agent is Employee emp)
        {
             // Update skills if exists and is an Employee
             emp.Specializations = JsonSerializer.Serialize(skills);
             emp.Language = lang;
             emp.Region = region;
             await _userManager.UpdateAsync(emp);
        }
        return agent;
    }

    private async Task<ApplicationUser> EnsureCustomerAsync(string email, string name, string lang)
    {
        var customer = await _userManager.FindByEmailAsync(email);
        if (customer == null)
        {
            customer = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = name.Split(' ')[0],
                LastName = name.Substring(name.IndexOf(' ') + 1),
                EmailConfirmed = true,
                Language = lang
            };
            await _userManager.CreateAsync(customer, "Password123!");
            await _userManager.AddToRoleAsync(customer, Constants.RoleCustomer);
        }
        return customer;
    }

    private async Task GenerateHistoryAsync(ApplicationUser customer, ApplicationUser agent, string title, string desc, int count, CancellationToken ct)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(ct);
        
        for (int i = 0; i < count; i++)
        {
            var ticket = new Ticket
            {
                Description = $"{title} #{i+1} - {desc}",
                CreatorGuid = Guid.Parse(customer.Id),
                ResponsibleId = agent.Id,
                CreationDate = DateTime.UtcNow.AddDays(-30 + i), // Spread over last 30 days
                CompletionDate = DateTime.UtcNow.AddDays(-30 + i + 1),
                Status = "Completed",
                PriorityScore = 50,
                EstimatedEffortPoints = 3,
                ProjectGuid = project?.Guid // Can be null
            };
            
            _context.Tickets.Add(ticket);
        }
        await _context.SaveChangesAsync(ct);
    }

    private async Task GeneratePendingTicketAsync(ApplicationUser customer, string title, string desc, CancellationToken ct)
    {
         var project = await _context.Projects.FirstOrDefaultAsync(ct);
         await _ticketService.CreateTicketAsync(
            description: $"{title} - {desc}",
            customerId: customer.Id,
            responsibleId: null,
            projectGuid: project?.Guid ?? Guid.Empty,
            completionTarget: DateTime.UtcNow.AddDays(2)
        );
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
