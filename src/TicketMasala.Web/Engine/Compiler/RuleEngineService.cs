using System.Security.Claims;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Workflow;

namespace TicketMasala.Web.Engine.Compiler;

/// <summary>
/// Facade over <see cref="ITicketWorkflowPolicy"/> for backward compatibility.
/// New code should call <see cref="Ticket.CanTransitionTo(Status, ITicketWorkflowPolicy, ITicketWorkflowContext)"/>
/// or <see cref="Ticket.GetValidNextStates(ITicketWorkflowPolicy, ITicketWorkflowContext)"/> directly.
/// </summary>
public class RuleEngineService : IRuleEngineService
{
    private readonly ITicketWorkflowPolicy _workflowPolicy;
    private readonly ILogger<RuleEngineService> _logger;

    public RuleEngineService(
        ITicketWorkflowPolicy workflowPolicy,
        ILogger<RuleEngineService> logger)
    {
        _workflowPolicy = workflowPolicy;
        _logger = logger;
    }

    public bool CanTransition(Ticket ticket, Status targetStatus, ClaimsPrincipal user)
    {
        var context = TicketWorkflowContext.FromClaimsPrincipal(user);
        return ticket.CanTransitionTo(targetStatus, _workflowPolicy, context);
    }

    public IEnumerable<Status> GetValidNextStates(Ticket ticket, ClaimsPrincipal user)
    {
        var context = TicketWorkflowContext.FromClaimsPrincipal(user);
        return ticket.GetValidNextStates(_workflowPolicy, context);
    }

    /// <summary>
    /// Validates that required fields are present for the target status transition.
    /// TODO: Implement compiled check for required fields based on domain configuration.
    /// Currently returns empty list (no validation).
    /// </summary>
    public IEnumerable<string> ValidateRequiredFields(Ticket ticket, Status targetStatus)
    {
        // TODO: #RULE-ENGINE - Implement required field validation
        // This should check domain configuration for required fields based on target status
        // and return any missing field names.
        return Enumerable.Empty<string>();
    }
}
