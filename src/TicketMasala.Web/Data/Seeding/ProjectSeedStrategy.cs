using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace TicketMasala.Web.Data.Seeding;

/// <summary>
/// Seed strategy for creating projects and project templates.
/// </summary>
public class ProjectSeedStrategy : ISeedStrategy
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<ProjectSeedStrategy> _logger;

    public ProjectSeedStrategy(
        MasalaDbContext context,
        ILogger<ProjectSeedStrategy> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ShouldSeedAsync()
    {
        // Seed if no templates exist
        var templateCount = await _context.ProjectTemplates.CountAsync();
        return templateCount == 0;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Seeding project templates...");

        await CreateProjectTemplatesAsync();

        _logger.LogInformation("Project templates seeded successfully");
    }

    private async Task CreateProjectTemplatesAsync()
    {
        var templates = new List<ProjectTemplate>
        {
            new ProjectTemplate
            {
                Guid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Standard Web Project",
                Description = "Standard template for new web development projects",
                Tickets = new List<TemplateTicket>
                {
                    new TemplateTicket { Description = "Setup Development Environment", EstimatedEffortPoints = 3, Priority = Priority.High, TicketType = TicketType.Task },
                    new TemplateTicket { Description = "Design Database Schema", EstimatedEffortPoints = 5, Priority = Priority.High, TicketType = TicketType.Task },
                    new TemplateTicket { Description = "Create API Endpoints", EstimatedEffortPoints = 8, Priority = Priority.Medium, TicketType = TicketType.Task },
                    new TemplateTicket { Description = "Implement Frontend UI", EstimatedEffortPoints = 13, Priority = Priority.Medium, TicketType = TicketType.Task }
                }
            },
            new ProjectTemplate
            {
                Guid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Mobile App Launch",
                Description = "Template for launching a new mobile application",
                Tickets = new List<TemplateTicket>
                {
                    new TemplateTicket { Description = "Design App Icon and Splash Screen", EstimatedEffortPoints = 3, Priority = Priority.Medium, TicketType = TicketType.Task },
                    new TemplateTicket { Description = "Setup Store Listings", EstimatedEffortPoints = 5, Priority = Priority.High, TicketType = TicketType.Task }
                }
            }
        };

        var newTemplatesCount = 0;
        foreach (var template in templates)
        {
            if (!await _context.ProjectTemplates.AnyAsync(t => t.Name == template.Name))
            {
                _context.ProjectTemplates.Add(template);
                newTemplatesCount++;
            }
        }

        if (newTemplatesCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created {Count} project templates", newTemplatesCount);
        }
    }
}
