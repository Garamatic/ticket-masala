using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using IT_Project2526.Repositories;
using IT_Project2526.Services.GERDA.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Services.Tickets;;

/// <summary>
/// Service responsible for building the dispatch backlog view.
/// Extracted from ManagerController to follow Single Responsibility.
/// Addresses God Object anti-pattern identified in architecture review.
/// </summary>
public interface IDispatchBacklogService
{
    Task<GerdaDispatchViewModel> BuildDispatchBacklogViewModelAsync();
}

public class DispatchBacklogService : IDispatchBacklogService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IDispatchingService _dispatchingService;
    private readonly ILogger<DispatchBacklogService> _logger;

    public DispatchBacklogService(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IDispatchingService dispatchingService,
        ILogger<DispatchBacklogService> logger)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _dispatchingService = dispatchingService;
        _logger = logger;
    }

    public async Task<GerdaDispatchViewModel> BuildDispatchBacklogViewModelAsync()
    {
        // Get unassigned or pending tickets  
        var allTickets = (await _ticketRepository.GetAllAsync()).ToList();
        var unassignedTickets = allTickets
            .Where(t => t.TicketStatus == Status.Pending || 
                       (t.TicketStatus == Status.Assigned && t.ResponsibleId == null))
            .OrderByDescending(t => t.CreationDate)
            .ToList();

        // Get all active employees
        var employees = (await _userRepository.GetAllEmployeesAsync()).ToList();

        // Get current workload for each employee
        var agentWorkloads = new Dictionary<string, (int count, int effortPoints)>();
        foreach (var employee in employees)
        {
            var tickets = await _ticketRepository.GetByResponsibleIdAsync(employee.Id);
            var activeTickets = tickets.Where(t => 
                t.TicketStatus != Status.Completed && 
                t.TicketStatus != Status.Failed).ToList();
            
            agentWorkloads[employee.Id] = (
                activeTickets.Count,
                activeTickets.Sum(t => t.EstimatedEffortPoints)
            );
        }

        // Get active projects
        var allProjects = await _projectRepository.GetAllAsync();
        var projects = allProjects
            .Where(p => p.Status == Status.Pending || p.Status == Status.InProgress)
            .OrderBy(p => p.Name)
            .ToList();

        // Build ticket dispatch info with recommendations
        var ticketDispatchInfos = new List<TicketDispatchInfo>();

        foreach (var ticket in unassignedTickets)
        {
            var ticketInfo = new TicketDispatchInfo
            {
                Guid = ticket.Guid,
                Description = ticket.Description,
                TicketStatus = ticket.TicketStatus,
                CreationDate = ticket.CreationDate,
                CompletionTarget = ticket.CompletionTarget,
                CustomerName = ticket.Customer != null 
                    ? $"{ticket.Customer.FirstName} {ticket.Customer.LastName}" 
                    : "Unknown",
                CustomerId = ticket.CustomerId,
                EstimatedEffortPoints = ticket.EstimatedEffortPoints,
                PriorityScore = ticket.PriorityScore,
                GerdaTags = ticket.GerdaTags,
                CurrentProjectGuid = ticket.ProjectGuid,
                CurrentProjectName = ticket.Project?.Name
            };

            // Get GERDA agent recommendations
            if (_dispatchingService.IsEnabled)
            {
                try
                {
                    var recommendations = await _dispatchingService.GetTopRecommendedAgentsAsync(ticket.Guid, count: 3);
                    
                    foreach (var recommendation in recommendations)
                    {
                        var agent = employees.FirstOrDefault(e => e.Id == recommendation.AgentId);
                        if (agent != null)
                        {
                            var workload = agentWorkloads.GetValueOrDefault(recommendation.AgentId, (0, 0));
                            
                            ticketInfo.RecommendedAgents.Add(new AgentRecommendation
                            {
                                AgentId = recommendation.AgentId,
                                AgentName = $"{agent.FirstName} {agent.LastName}",
                                Team = agent.Team,
                                Score = recommendation.Score,
                                CurrentWorkload = workload.Item1,
                                MaxCapacity = agent.MaxCapacityPoints,
                                Specializations = agent.Specializations,
                                Language = agent.Language,
                                Region = agent.Region
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get GERDA recommendations for ticket {TicketGuid}", ticket.Guid);
                }
            }

            // Recommend project based on customer
            if (ticket.ProjectGuid == null && ticket.Customer != null)
            {
                var customerProjects = projects.Where(p => p.CustomerId == ticket.CustomerId).ToList();
                if (customerProjects.Any())
                {
                    // Recommend the most recent active project for this customer
                    var recommendedProject = customerProjects
                        .OrderByDescending(p => p.CreationDate)
                        .FirstOrDefault();
                    
                    if (recommendedProject != null)
                    {
                        ticketInfo.RecommendedProjectGuid = recommendedProject.Guid;
                        ticketInfo.RecommendedProjectName = recommendedProject.Name;
                    }
                }
            }

            ticketDispatchInfos.Add(ticketInfo);
        }

        // Build agent info list
        var agentInfos = employees.Select(e =>
        {
            var workload = agentWorkloads.GetValueOrDefault(e.Id, (0, 0));
            return new AgentInfo
            {
                Id = e.Id,
                Name = $"{e.FirstName} {e.LastName}",
                Team = e.Team,
                CurrentWorkload = workload.Item1,
                MaxCapacity = 10, // Default max tickets
                CurrentEffortPoints = workload.Item2,
                MaxCapacityPoints = e.MaxCapacityPoints,
                Language = e.Language,
                Region = e.Region
            };
        }).OrderBy(a => a.Team).ThenBy(a => a.Name).ToList();

        // Calculate statistics
        var now = DateTime.UtcNow;
        var statistics = new DispatchStatistics
        {
            TotalUnassignedTickets = ticketDispatchInfos.Count,
            TicketsWithProjectRecommendation = ticketDispatchInfos.Count(t => t.RecommendedProjectGuid.HasValue),
            TicketsWithAgentRecommendation = ticketDispatchInfos.Count(t => t.RecommendedAgents.Any()),
            TotalAvailableAgents = agentInfos.Count(a => a.IsAvailable),
            OverloadedAgents = agentInfos.Count(a => a.WorkloadPercentage >= 100),
            AverageTicketAge = unassignedTickets.Any() 
                ? unassignedTickets.Average(t => (now - t.CreationDate).TotalHours) 
                : 0,
            HighPriorityTickets = ticketDispatchInfos.Count(t => t.PriorityScore >= 50),
            TicketsOlderThan24Hours = ticketDispatchInfos.Count(t => t.TimeInBacklog.TotalHours >= 24)
        };

        // Build project options
        var projectOptions = projects.Select(p => new ProjectOption
        {
            Guid = p.Guid,
            Name = p.Name,
            CustomerName = p.Customer != null 
                ? $"{p.Customer.FirstName} {p.Customer.LastName}" 
                : "Unknown",
            CurrentTicketCount = p.Tasks.Count,
            Status = p.Status
        }).ToList();

        return new GerdaDispatchViewModel
        {
            UnassignedTickets = ticketDispatchInfos,
            AvailableAgents = agentInfos,
            Statistics = statistics,
            Projects = projectOptions
        };
    }
}
