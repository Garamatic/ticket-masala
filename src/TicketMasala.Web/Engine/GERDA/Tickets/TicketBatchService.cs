using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
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
    private readonly ITicketLifecycle _ticketLifecycle;
    private readonly ILogger<TicketBatchService> _logger;

    public TicketBatchService(
        ITicketLifecycle ticketLifecycle,
        ILogger<TicketBatchService> logger)
    {
        _ticketLifecycle = ticketLifecycle;
        _logger = logger;
    }

    public async Task<BatchAssignResult> BatchAssignTicketsAsync(
        BatchAssignRequest request,
        Func<Guid, Task<string?>> getRecommendedAgent)
    {
        var result = new BatchAssignResult();

        foreach (var ticketGuid in request.TicketGuids)
        {
            try
            {
                var (assignedAgentId, assignedProjectGuid) = await DetermineAssignmentAsync(
                    ticketGuid, request, getRecommendedAgent);

                if (string.IsNullOrEmpty(assignedAgentId) && !assignedProjectGuid.HasValue)
                {
                    RecordFailure(result, ticketGuid, "no assignment determined");
                    continue;
                }

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
            var result = await _ticketLifecycle.ExecuteAsync(
                new AssignTicketCommand(id, agentId),
                new TicketContext("system"));

            if (!result.Success)
            {
                _logger.LogWarning("Batch assign failed for ticket {TicketGuid}: {Error}", id, result.ErrorMessage);
            }
        }
    }

    public async Task BatchUpdateStatusAsync(List<Guid> ticketIds, Status status)
    {
        foreach (var id in ticketIds)
        {
            var result = await _ticketLifecycle.ExecuteAsync(
                new TransitionStatusCommand(id, status),
                new TicketContext("system"));

            if (!result.Success)
            {
                _logger.LogWarning("Batch status update failed for ticket {TicketGuid}: {Error}", id, result.ErrorMessage);
            }
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
        Func<Guid, Task<string?>> getRecommendedAgent)
    {
        if (request.UseGerdaRecommendations)
        {
            var agentId = await getRecommendedAgent(ticketGuid);
            return (agentId, request.ForceProjectGuid);
        }

        return (request.ForceAgentId, request.ForceProjectGuid);
    }
}
