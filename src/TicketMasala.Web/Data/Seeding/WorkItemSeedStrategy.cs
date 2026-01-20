using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TicketMasala.Web.Configuration;

namespace TicketMasala.Web.Data.Seeding;

/// <summary>
/// Seed strategy for creating projects and work items (tickets) from configuration.
/// </summary>
public class WorkItemSeedStrategy : ISeedStrategy
{
    private readonly MasalaDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<WorkItemSeedStrategy> _logger;

    public WorkItemSeedStrategy(
        MasalaDbContext context,
        IWebHostEnvironment environment,
        ILogger<WorkItemSeedStrategy> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<bool> ShouldSeedAsync()
    {
        // Always run to ensure new items from config are added
        return true;
    }

    public async Task SeedAsync()
    {
        var config = await LoadSeedConfigurationAsync();
        if (config == null)
        {
            return;
        }

        // 1. Seed Projects (WorkContainers)
        if (config.WorkContainers?.Count > 0)
        {
            _logger.LogInformation("Seeding {Count} projects...", config.WorkContainers.Count);
            foreach (var containerDto in config.WorkContainers)
            {
                await CreateOrUpdateProjectAsync(containerDto);
            }
        }

        // 2. Seed Unassigned Work Items
        if (config.UnassignedWorkItems?.Count > 0)
        {
            _logger.LogInformation("Seeding {Count} unassigned work items...", config.UnassignedWorkItems.Count);
            foreach (var itemDto in config.UnassignedWorkItems)
            {
                await CreateOrUpdateWorkItemAsync(itemDto, null);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task<SeedConfig?> LoadSeedConfigurationAsync()
    {
        var seedFilePath = ConfigurationPaths.GetConfigFilePath(
            _environment.ContentRootPath,
            "seed_data.json");
        
        if (!File.Exists(seedFilePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(seedFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<SeedConfig>(json, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing seed data JSON from {Path}", seedFilePath);
            return null;
        }
    }

    private async Task CreateOrUpdateProjectAsync(SeedWorkContainer containerDto)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(c => c.Name == containerDto.Name);

        if (project == null)
        {
            // Create new Project
            project = new Project
            {
                Name = containerDto.Name,
                Description = containerDto.Description,
                Status = containerDto.Status,
                CreationDate = DateTime.UtcNow,
            };
            
            // Resolve ProjectManager
            if (!string.IsNullOrEmpty(containerDto.ProjectManagerEmail))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == containerDto.ProjectManagerEmail);
                if (user != null)
                {
                    project.ProjectManagerId = user.Id;
                }
            }

            // Resolve Customer
            if (!string.IsNullOrEmpty(containerDto.CustomerEmail))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == containerDto.CustomerEmail);
                if (user != null)
                {
                    project.CustomerId = user.Id;
                }
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync(); // Save to get Guid
            _logger.LogInformation("Created project: {Name}", project.Name);
        }

        // Seed items within the project
        if (containerDto.WorkItems?.Count > 0)
        {
            foreach (var itemDto in containerDto.WorkItems)
            {
                await CreateOrUpdateWorkItemAsync(itemDto, project.Guid);
            }
        }
    }

    private async Task CreateOrUpdateWorkItemAsync(SeedWorkItem itemDto, Guid? projectId)
    {
        // Use Description as Title since Title is missing in JSON
        var title = itemDto.Description.Length > 50 ? itemDto.Description.Substring(0, 47) + "..." : itemDto.Description;
        
        // Check for existence 
        // Note: We use Title match. If duplicate descriptions exist, this might skip.
        var exists = await _context.Tickets
            .AnyAsync(w => w.Title == title && w.ProjectGuid == projectId);

        if (exists)
        {
            return;
        }

        var ticket = new Ticket
        {
            Title = title,
            Description = itemDto.Description,
            TicketType = itemDto.Type,
            WorkItemTypeCode = itemDto.Type.ToString(), // Sync field
            TicketStatus = itemDto.Status,
            Status = itemDto.Status.ToString(), // Sync field
            ProjectGuid = projectId,
            CreationDate = DateTime.UtcNow.AddDays(-itemDto.CompletionDaysAgo ?? 0),
            PriorityScore = itemDto.PriorityScore ?? 0,
            EstimatedEffortPoints = (int)(itemDto.EstimatedEffortPoints ?? 0),
            GerdaTags = itemDto.GerdaTags
        };

        // Resolve Responsible (AssignedTo)
        if (!string.IsNullOrEmpty(itemDto.ResponsibleEmail))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == itemDto.ResponsibleEmail);
            if (user != null)
            {
                ticket.ResponsibleId = user.Id;
            }
        }
        
        // Resolve Customer (Reporter)
        if (!string.IsNullOrEmpty(itemDto.CustomerEmail)) 
        {
             var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == itemDto.CustomerEmail);
             if (user != null)
             {
                 ticket.CustomerId = user.Id;
             }
        }

        _context.Tickets.Add(ticket);
        _logger.LogInformation("Creating ticket: {Title}", ticket.Title);
        
        await _context.SaveChangesAsync();

        if (itemDto.Comments?.Count > 0)
        {
            foreach (var commentDto in itemDto.Comments)
            {
                var comment = new TicketComment
                {
                    TicketId = ticket.Guid,
                    Body = commentDto.Body,
                    CreatedAt = DateTime.UtcNow.AddDays(-commentDto.CreatedDaysAgo),
                };
                
                 if (!string.IsNullOrEmpty(commentDto.AuthorEmail)) 
                {
                     var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == commentDto.AuthorEmail);
                     if (user != null)
                     {
                         comment.AuthorId = user.Id;
                     }
                }
                
                _context.TicketComments.Add(comment);
            }
            await _context.SaveChangesAsync();
        }
    }
}
