using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Data;
using TicketMasala.Web.AI;

namespace TicketMasala.Web.Engine.Projects;

public class ProjectTemplateService : IProjectTemplateService
{
    private readonly MasalaDbContext _context;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<ProjectTemplateService> _logger;

    public ProjectTemplateService(
        MasalaDbContext context,
        IOpenAiService openAiService,
        ILogger<ProjectTemplateService> logger)
    {
        _context = context;
        _openAiService = openAiService;
        _logger = logger;
    }

    public async Task ApplyTemplateAsync(Project project, Guid templateId)
    {
        var template = await _context.ProjectTemplates
            .Include(t => t.Tickets)
            .FirstOrDefaultAsync(t => t.Guid == templateId);

        if (template != null)
        {
            foreach (var templateTicket in template.Tickets)
            {
                // Generate AI summary for each ticket
                string? summary = null;
                try
                {
                    summary = await _openAiService.GetResponseAsync(OpenAIPrompts.Summary, templateTicket.Description);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate AI summary for template ticket");
                }

                var ticket = new Ticket
                {
                    Guid = Guid.NewGuid(),
                    Title = templateTicket.Description.Length > 100
                        ? templateTicket.Description.Substring(0, 100)
                        : templateTicket.Description,
                    Description = templateTicket.Description,
                    DomainId = "IT",
                    Status = "New",
                    EstimatedEffortPoints = templateTicket.EstimatedEffortPoints,
                    PriorityScore = (double)templateTicket.Priority * 25,
                    TicketType = templateTicket.TicketType,
                    TicketStatus = Status.Pending,
                    CreationDate = DateTime.UtcNow,
                    CreatorGuid = project.CreatorGuid,
                    Customer = project.Customers.FirstOrDefault(), // Use the primary customer
                    CustomerId = project.CustomerId,
                    Project = project,
                    ProjectGuid = project.Guid,
                    AiSummary = summary,
                };
                _context.Tickets.Add(ticket);
            }
            await _context.SaveChangesAsync();
        }
    }
}
