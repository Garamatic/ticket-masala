using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
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
    Task<GerdaDispatchViewModel> BuildDispatchBacklogViewModelAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
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

    public async Task<GerdaDispatchViewModel> BuildDispatchBacklogViewModelAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Get all pending tickets
        // Note: In a real high-scale scenario, we would paginate at the DB level (Repository).
        // For now, fetching all pending is acceptable as backlog size is managed.
        var allTickets = (await _ticketRepository.GetAllAsync()).ToList();

        var pendingTickets = allTickets
            .Where(t => t.TicketStatus == Status.Pending ||
                       (t.TicketStatus == Status.Assigned && t.ResponsibleId == null))
            .OrderByDescending(t => t.CreationDate) // Prioritize newest or oldest? Usually oldest first, but let's stick to existing
            .ToList();

        // 2. Pagination Logic
        int totalItems = pendingTickets.Count;
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var pagedTickets = pendingTickets
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // 3. Get employees (agents)
        var employees = (await _userRepository.GetAllEmployeesAsync()).ToList();

        // 4. Calculate workloads
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

        // 5. Get active projects
        var allProjects = await _projectRepository.GetAllAsync();
        var projects = allProjects
            .Where(p => p.Status == Status.Pending || p.Status == Status.InProgress)
            .OrderBy(p => p.Name)
            .ToList();

        // 6. Build ViewModel for *Paged* Tickets
        var ticketDispatchInfos = pagedTickets.Select(t => new TicketDispatchInfo
        {
            Guid = t.Guid,
            Description = t.Description,
            EstimatedEffortPoints = t.EstimatedEffortPoints,
            PriorityScore = t.PriorityScore,
            RecommendedProjectName = t.RecommendedProjectName,
            CurrentProjectName = t.CurrentProjectName,
            TicketStatus = t.TicketStatus,
            Status = t.TicketStatus,
            CreationDate = t.CreationDate,
            CompletionTarget = t.CompletionTarget,
            CustomerName = t.Customer != null ? $"{t.Customer.FirstName} {t.Customer.LastName}" : "Unknown",
            CustomerId = t.CustomerId,
            GerdaTags = t.GerdaTags,
            // RecommendedProjectGuid not present on Ticket entity, inferred from name or left null for now
            CurrentProjectGuid = t.ProjectGuid
        }).ToList();

        // 7. Get Recommendations for current page
        if (_dispatchingService.IsEnabled)
        {
            // Create lookup for efficiency
            var employeeMap = employees.ToDictionary(e => e.Id);

            foreach (var info in ticketDispatchInfos)
            {
                try
                {
                    var recommendations = await _dispatchingService.GetTopRecommendedAgentsAsync(info.Guid, 3);
                    if (recommendations != null && recommendations.Any())
                    {
                        info.RecommendedAgents = recommendations
                            .Select(r =>
                            {
                                if (!employeeMap.TryGetValue(r.AgentId, out var agent)) return null;

                                var workload = agentWorkloads.GetValueOrDefault(r.AgentId, (0, 0));

                                return new AgentRecommendation
                                {
                                    AgentId = r.AgentId,
                                    AgentName = $"{agent.FirstName} {agent.LastName}",
                                    Score = r.Score,
                                    Team = agent.Team,
                                    CurrentWorkload = workload.Item1,
                                    MaxCapacity = agent.MaxCapacityPoints,
                                    Specializations = agent.Specializations,
                                    Language = agent.Language,
                                    Region = agent.Region,
                                    Reasons = r.Reasons
                                };
                            })
                            .Where(r => r != null)
                            .ToList()!;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get GERDA recommendations for ticket {TicketId}", info.Guid);
                }
            }
        }

        // 8. Build Agent Infos
        var agentInfos = employees.Select(e =>
        {
            var workload = agentWorkloads.GetValueOrDefault(e.Id, (0, 0));
            return new AgentInfo
            {
                Id = e.Id,
                Name = $"{e.FirstName} {e.LastName}",
                Team = e.Team,
                CurrentWorkload = workload.Item1,
                MaxCapacity = 10,
                CurrentEffortPoints = workload.Item2,
                MaxCapacityPoints = e.MaxCapacityPoints,
                Language = e.Language,
                Region = e.Region
            };
        }).OrderBy(a => a.Team).ThenBy(a => a.Name).ToList();

        // 9. Statistics (Global, not paged)
        var now = DateTime.UtcNow;
        var statistics = new DispatchStatistics
        {
            TotalUnassignedTickets = totalItems,
            TicketsWithProjectRecommendation = pendingTickets.Count(t => !string.IsNullOrEmpty(t.RecommendedProjectName)),
            TicketsWithAgentRecommendation = 0, // Hard to calc without running AI on all, 0 is safer fallback
            TotalAvailableAgents = agentInfos.Count(a => a.IsAvailable),
            OverloadedAgents = agentInfos.Count(a => a.WorkloadPercentage >= 100),
            AverageTicketAge = pendingTickets.Any()
                ? pendingTickets.Average(t => (now - t.CreationDate).TotalHours)
                : 0,
            HighPriorityTickets = pendingTickets.Count(t => t.PriorityScore >= 50),
            TicketsOlderThan24Hours = pendingTickets.Count(t => (now - t.CreationDate).TotalHours >= 24)
        };

        var projectOptions = projects.Select(p => new ProjectOption
        {
            Guid = p.Guid,
            Name = p.Name,
            CustomerName = p.Customer != null ? $"{p.Customer.FirstName} {p.Customer.LastName}" : "Unknown",
            CurrentTicketCount = p.Tasks.Count,
            Status = p.Status
        }).ToList();

        return new GerdaDispatchViewModel
        {
            UnassignedTickets = ticketDispatchInfos,
            AvailableAgents = agentInfos,
            Statistics = statistics,
            Projects = projectOptions,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            LastModelTrainingTime = _dispatchingService.LastModelTrainingTime
        };
    }
}
