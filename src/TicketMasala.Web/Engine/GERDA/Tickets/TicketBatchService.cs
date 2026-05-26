using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.ViewModels.GERDA;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

public interface ITicketBatchService
{
    Task<BatchAssignResult> BatchAssignTicketsAsync(BatchAssignRequest request, Func<Guid, Task<string?>> getRecommendedAgent);
    Task BatchAssignToAgentAsync(List<Guid> ticketIds, string agentId);
    Task BatchUpdateStatusAsync(List<Guid> ticketIds, Status status);
}

/// <summary>
/// Batch ticket coordinator. All mutations delegate to <see cref="ITicketLifecycle"/>
/// so that observer notification, audit logging, and outbox publishing happen
/// consistently for every ticket.
/// </summary>
public class TicketBatchService : ITicketBatchService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITicketLifecycle _ticketLifecycle;
    private readonly ILogger<TicketBatchService> _logger;

    public TicketBatchService(
        IProjectRepository projectRepository,
        ITicketLifecycle ticketLifecycle,
        ILogger<TicketBatchService> logger)
    {
        _projectRepository = projectRepository;
        _ticketLifecycle = ticketLifecycle;
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
                var (assignedAgentId, assignedProjectGuid) = await DetermineAssignmentAsync(
                    ticketGuid, request, getRecommendedAgent, projectLookup);

                if (string.IsNullOrEmpty(assignedAgentId) && !assignedProjectGuid.HasValue)
                {
                    RecordFailure(result, ticketGuid, "no assignment determined");
                    continue;
                }

                // Delegate to TicketLifecycle deep module — all choreography (audit, observers, outbox) included
                var lifecycleResult = await _ticketLifecycle.ExecuteAsync(
                    new AssignTicketCommand(ticketGuid, assignedAgentId, assignedProjectGuid),
                    new TicketContext("system"));

                if (!lifecycleResult.Success)
                {
                    RecordFailure(result, ticketGuid, lifecycleResult.ErrorMessage ?? "Assignment failed");
                    continue;
                }

                RecordSuccess(result, ticketGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning ticket {TicketGuid}", ticketGuid);
                RecordFailure(result, ticketGuid, ex.Message);
            }
        }

        return result;
    }

    public async Task BatchAssignToAgentAsync(List<Guid> ticketIds, string agentId)
    {
        foreach (var id in ticketIds)
        {
            await _ticketLifecycle.ExecuteAsync(
                new AssignTicketCommand(id, agentId),
                new TicketContext("system"));
        }
    }

    public async Task BatchUpdateStatusAsync(List<Guid> ticketIds, Status status)
    {
        foreach (var id in ticketIds)
        {
            await _ticketLifecycle.ExecuteAsync(
                new TransitionStatusCommand(id, status),
                new TicketContext("system"));
        }
    }

    private static void RecordFailure(BatchAssignResult result, Guid ticketGuid, string error)
    {
        result.FailureCount++;
        result.Errors.Add($"Ticket {ticketGuid}: {error}");
        result.Assignments.Add(new TicketAssignmentDetail
        {
            TicketGuid = ticketGuid,
            Success = false,
            ErrorMessage = error
        });
    }

    private static void RecordSuccess(BatchAssignResult result, Guid ticketGuid)
    {
        result.SuccessCount++;
        result.Assignments.Add(new TicketAssignmentDetail
        {
            TicketGuid = ticketGuid,
            Success = true
        });
    }

    private async Task<(string? AgentId, Guid? ProjectId)> DetermineAssignmentAsync(
        Guid ticketGuid,
        BatchAssignRequest request,
        Func<Guid, Task<string?>> getRecommendedAgent,
        Dictionary<string, Guid> projectLookup)
    {
        if (request.UseGerdaRecommendations)
        {
            var agentId = await getRecommendedAgent(ticketGuid);

            // Note: project determination requires ticket data; lifecycle command
            // only supports agent + project. Project lookup is best-effort here.
            // For full GERDA project recommendation, use the GERDA pipeline directly.
            Guid? projectGuid = request.ForceProjectGuid;
            return (agentId, projectGuid);
        }

        return (request.ForceAgentId, request.ForceProjectGuid);
    }
}
