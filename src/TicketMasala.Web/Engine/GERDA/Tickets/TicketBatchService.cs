using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Observers;
using TicketMasala.Web.ViewModels.GERDA;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

public interface ITicketBatchService
{
    Task<BatchAssignResult> BatchAssignTicketsAsync(BatchAssignRequest request, Func<Guid, Task<string?>> getRecommendedAgent);
    Task BatchAssignToAgentAsync(List<Guid> ticketIds, string agentId);
    Task BatchUpdateStatusAsync(List<Guid> ticketIds, Status status);
}

public class TicketBatchService : ITicketBatchService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEnumerable<ITicketObserver> _observers;
    private readonly ITicketWorkflowService _ticketWorkflowService;
    private readonly ILogger<TicketBatchService> _logger;

    public TicketBatchService(
        ITicketRepository ticketRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IEnumerable<ITicketObserver> observers,
        ITicketWorkflowService ticketWorkflowService,
        ILogger<TicketBatchService> logger)
    {
        _ticketRepository = ticketRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _observers = observers;
        _ticketWorkflowService = ticketWorkflowService;
        _logger = logger;
    }

    public async Task<BatchAssignResult> BatchAssignTicketsAsync(
        BatchAssignRequest request,
        Func<Guid, Task<string?>> getRecommendedAgent)
    {
        var result = new BatchAssignResult();

        var allProjects = await _projectRepository.GetActiveProjectsAsync();
        var projectLookup = allProjects.ToDictionary(p => p.Name, p => p.Guid, StringComparer.OrdinalIgnoreCase);

        foreach (var ticketGuid in request.TicketGuids)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: true);

                if (ticket == null)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Ticket {ticketGuid} not found");
                    continue;
                }

                string? assignedAgentId = null;
                Guid? assignedProjectGuid = null;

                (assignedAgentId, assignedProjectGuid) = await DetermineAssignmentStrategyAsync(ticket, request, getRecommendedAgent, projectLookup);

                Employee? assignedAgent = null;
                if (!string.IsNullOrEmpty(assignedAgentId) || assignedProjectGuid.HasValue)
                {
                    assignedAgent = await ApplyAssignmentAsync(ticket, assignedAgentId, assignedProjectGuid, request.UseGerdaRecommendations);
                }

                var assignedProject = assignedProjectGuid.HasValue
                    ? await _projectRepository.GetByIdAsync(assignedProjectGuid.Value, includeRelations: false)
                    : null;

                result.SuccessCount++;
                result.Assignments.Add(new TicketAssignmentDetail
                {
                    TicketGuid = ticketGuid,
                    AssignedAgentName = assignedAgent != null
                        ? $"{assignedAgent.FirstName} {assignedAgent.LastName}"
                        : null,
                    AssignedProjectName = assignedProject?.Name,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning ticket {TicketGuid}", ticketGuid);
                result.FailureCount++;
                result.Errors.Add($"Error assigning ticket {ticketGuid}: {ex.Message}");
                result.Assignments.Add(new TicketAssignmentDetail
                {
                    TicketGuid = ticketGuid,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        return result;
    }

    public async Task BatchAssignToAgentAsync(List<Guid> ticketIds, string agentId)
    {
        foreach (var id in ticketIds)
        {
            await _ticketWorkflowService.AssignTicketAsync(id, agentId);
        }
    }

    public async Task BatchUpdateStatusAsync(List<Guid> ticketIds, Status status)
    {
        foreach (var id in ticketIds)
        {
            var ticket = await _ticketRepository.GetByIdAsync(id, includeRelations: false);
            if (ticket != null)
            {
                ticket.TicketStatus = status;
                await _ticketWorkflowService.UpdateTicketAsync(ticket);
            }
        }
    }

    private async Task<(string? AgentId, Guid? ProjectId)> DetermineAssignmentStrategyAsync(
        Ticket ticket,
        BatchAssignRequest request,
        Func<Guid, Task<string?>> getRecommendedAgent,
        Dictionary<string, Guid> projectLookup)
    {
        string? assignedAgentId = null;
        Guid? assignedProjectGuid = null;

        if (request.UseGerdaRecommendations)
        {
            assignedAgentId = await getRecommendedAgent(ticket.Guid);

            if (!string.IsNullOrEmpty(ticket.RecommendedProjectName) && projectLookup.TryGetValue(ticket.RecommendedProjectName, out var projGuid))
            {
                assignedProjectGuid = projGuid;
            }
            else if (ticket.ProjectGuid == null && ticket.CustomerId != null)
            {
                var recommendedProject = await _projectRepository.GetRecommendedProjectForCustomerAsync(ticket.CustomerId);
                assignedProjectGuid = recommendedProject?.Guid;
            }
        }
        else
        {
            assignedAgentId = request.ForceAgentId;
            assignedProjectGuid = request.ForceProjectGuid;
        }

        return (assignedAgentId, assignedProjectGuid);
    }

    private async Task<Employee?> ApplyAssignmentAsync(
        Ticket ticket,
        string? agentId,
        Guid? projectId,
        bool isRecommendation)
    {
        Employee? assignedAgent = null;

        if (!string.IsNullOrEmpty(agentId))
        {
            assignedAgent = await _userRepository.GetEmployeeByIdAsync(agentId);
            if (assignedAgent != null)
            {
                ticket.ResponsibleId = agentId;
                ticket.TicketStatus = Status.Assigned;

                if (isRecommendation)
                {
                    ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
                        ? "AI-Dispatched"
                        : $"{ticket.GerdaTags},AI-Dispatched";
                }
            }
        }

        if (projectId.HasValue)
        {
            ticket.ProjectGuid = projectId.Value;
        }

        await _ticketRepository.UpdateAsync(ticket);

        if (assignedAgent != null)
        {
            await NotifyObserversAssignedAsync(ticket, assignedAgent);
        }
        else if (projectId.HasValue)
        {
            await NotifyObserversUpdatedAsync(ticket);
        }

        return assignedAgent;
    }

    private async Task NotifyObserversAssignedAsync(Ticket ticket, Employee assignee)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketAssignedAsync(ticket, assignee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed on ticket assignment", observer.GetType().Name);
            }
        }
    }

    private async Task NotifyObserversUpdatedAsync(Ticket ticket)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketUpdatedAsync(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed on ticket update", observer.GetType().Name);
            }
        }
    }
}
