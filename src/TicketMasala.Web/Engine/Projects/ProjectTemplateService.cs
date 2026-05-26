using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Ports;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Data;

namespace TicketMasala.Web.Engine.Projects;

public class ProjectTemplateService : IProjectTemplateService
{
    private readonly MasalaDbContext _context;
    private readonly IAIGenerationPort _aiPort;
    private readonly ISystemClock _clock;
    private readonly ILogger<ProjectTemplateService> _logger;

    public ProjectTemplateService(
        MasalaDbContext context,
        IAIGenerationPort aiPort,
        ISystemClock clock,
        ILogger<ProjectTemplateService> logger)
    {
        _context = context;
        _aiPort = aiPort;
        _clock = clock;
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
                    var result = await _aiPort.CompleteAsync(
                        new AICompletionRequest
                        {
                            Operation = "summarize",
                            Content = templateTicket.Description,
                        });
                    summary = result.Text;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate AI summary for template ticket");
                }

                var ticket = new Ticket
                {
                    Title = templateTicket.Description.Length > 100
                        ? templateTicket.Description.Substring(0, 100)
                        : templateTicket.Description,
                    Description = templateTicket.Description,
                    DomainId = "IT",
                    EstimatedEffortPoints = templateTicket.EstimatedEffortPoints,
                    PriorityScore = (double)templateTicket.Priority * 25,
                    TicketType = templateTicket.TicketType,
                    TicketStatus = Status.Pending,
                    CreatorGuid = project.CreatorGuid,
                    Customer = project.Customers.FirstOrDefault(), // Use the primary customer
                    CustomerId = project.CustomerId,
                    Project = project,
                    ProjectGuid = project.Guid,
                    AiSummary = summary,
                };
                ticket.SyncStatus();
                _context.Tickets.Add(ticket);
            }
            await _context.SaveChangesAsync();
        }
    }
}
