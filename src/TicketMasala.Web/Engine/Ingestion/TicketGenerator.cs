using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using TicketMasala.Web.Utilities;

namespace TicketMasala.Web.Engine.Ingestion;

public class TicketGenerator : ITicketGenerator
{
    private readonly ITicketLifecycle _ticketLifecycle;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MasalaDbContext _context;
    private readonly ILogger<TicketGenerator> _logger;
    private readonly ISystemClock _clock;


    public TicketGenerator(
        ITicketLifecycle ticketLifecycle,
        UserManager<ApplicationUser> userManager,
        MasalaDbContext context,
        ILogger<TicketGenerator> logger,
        ISystemClock clock)
    {
        _ticketLifecycle = ticketLifecycle;
        _userManager = userManager;
        _context = context;
        _logger = logger;
        _clock = clock;
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
                Description = $"{title} #{i + 1} - {desc}",
                CreatorGuid = Guid.Parse(customer.Id),
                ResponsibleId = agent.Id,
                CompletionDate = _clock.UtcNow.AddDays(-30 + i + 1),
                TicketStatus = Status.Completed,
                PriorityScore = 50,
                EstimatedEffortPoints = 3,
                ProjectGuid = project?.Guid // Can be null
            };
            ticket.SyncStatus();

            _context.Tickets.Add(ticket);
        }
        await _context.SaveChangesAsync(ct);
    }

    private async Task GeneratePendingTicketAsync(ApplicationUser customer, string title, string desc, CancellationToken ct)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(ct);
        await _ticketLifecycle.ExecuteAsync(
           new CreateTicketCommand($"{title} - {desc}", customer.Id, null, project?.Guid ?? Guid.Empty, _clock.UtcNow.AddDays(2)),
           new TicketContext(customer.Id));
    }

    public async Task GenerateRandomTicketAsync(CancellationToken cancellationToken = default)
    {
        // Get a random customer
        var customers = await _userManager.GetUsersInRoleAsync(Constants.RoleCustomer);
        if (customers.Count == 0)
            return;

        var randomCustomer = customers[Random.Shared.Next(customers.Count)];

        // Get a random project safely
        Project? project = null;

        // Strategy 1: Try to find a project for this customer
        var customerProjectIds = await _context.Projects
            .Where(p => p.Customers.Any(c => c.Id == randomCustomer.Id))
            .Select(p => p.Guid)
            .ToListAsync(cancellationToken);

        if (customerProjectIds.Any())
        {
            var randomProjectId = customerProjectIds[Random.Shared.Next(customerProjectIds.Count)];
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
                var randomProjectId = allProjectIds[Random.Shared.Next(allProjectIds.Count)];
                project = await _context.Projects
                   .Include(p => p.ProjectManager)
                   .FirstOrDefaultAsync(p => p.Guid == randomProjectId, cancellationToken);
            }
        }

        if (project == null)
            return; // No projects exist

        var title = RandomDataHelper.GenerateTicketTitle();
        var description = RandomDataHelper.GenerateTicketDescription();

        // Create ticket using the service method which handles defaults and notifications
        var result = await _ticketLifecycle.ExecuteAsync(
            new CreateTicketCommand(
                $"{title} - {description}",
                randomCustomer.Id,
                null,
                project.Guid,
                _clock.UtcNow.AddDays(Random.Shared.Next(1, 14))),
            new TicketContext(randomCustomer.Id));

        if (!result.Success)
        {
            _logger.LogWarning("Random ticket generation failed: {Error}", result.ErrorMessage);
            return;
        }

        var ticket = result.Ticket!;

        // Enhance with random priority
        ticket.PriorityScore = Random.Shared.NextDouble() * 100;

        // Direct field mutation - no full update pipeline needed for seed data
        // TODO: use UpdateTicketCommand when it supports full field updates
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated random ticket: {Title} for Customer: {Customer}", title, randomCustomer.UserName);
    }
}
