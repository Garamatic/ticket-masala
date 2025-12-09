using TicketMasala.Web.Models;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.ViewModels.GERDA;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service responsible for building the dispatch backlog view.
/// Extracted from ManagerController to follow Single Responsibility.
/// Addresses God Object anti-pattern identified in architecture review.
/// </summary>
public interface IDispatchBacklogService
{
    Task<GerdaDispatchViewModel> BuildDispatchBacklogViewModelAsync(CancellationToken cancellationToken = default);
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

    public async Task<GerdaDispatchViewModel> BuildDispatchBacklogViewModelAsync(CancellationToken cancellationToken = default)
    {
        // Check cancellation early
        cancellationToken.ThrowIfCancellationRequested();

        // Get unassigned or pending tickets  
        // Note: Repository methods might not support cancellation token yet, but we can check token between calls
        // Optimally repositories should be updated too, but we will start here for 499 prevention in typical slow paths.
        
        // Assuming repository methods are not yet updated to accept token, we can't pass it down easily without refactoring repositories.
        // For this sprint item, we will check cancellation token at critical points.
        
        var allTickets = (await _ticketRepository.GetAllAsync()).ToList();
        cancellationToken.ThrowIfCancellationRequested();
        
        var unassignedTickets = allTickets
            .Where(t => t.TicketStatus == Status.Pending || 
                       (t.TicketStatus == Status.Assigned && t.ResponsibleId == null))
            .OrderByDescending(t => t.CreationDate)
            .ToList();

        // Get all active employees
        var employees = (await _userRepository.GetAllEmployeesAsync()).ToList();
        cancellationToken.ThrowIfCancellationRequested();

        // Get current workload for each employee
        var agentWorkloads = new Dictionary<string, (int count, int effortPoints)>();
        foreach (var employee in employees)
        {
            // Frequent check inside loop
            cancellationToken.ThrowIfCancellationRequested();
            
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
        // Map unassigned tickets to TicketDispatchInfo
        var ticketDispatchInfos = unassignedTickets.Select(t => new TicketDispatchInfo
        {
            Guid = t.Guid,
            Description = t.Description,
            EstimatedEffortPoints = t.EstimatedEffortPoints,
            PriorityScore = t.PriorityScore,
            RecommendedProjectName = t.RecommendedProjectName,
            CurrentProjectName = t.CurrentProjectName
        }).ToList();

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
