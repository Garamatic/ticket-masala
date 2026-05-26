using System.Security.Claims;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Configuration;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Workflow;
using TicketMasala.Web.Engine.GERDA.Configuration;

namespace TicketMasala.Web.Engine.Compiler;

/// <summary>
/// Production implementation of <see cref="ITicketWorkflowPolicy"/>.
/// Uses domain configuration and compiled transition rules to evaluate
/// whether a user may transition a ticket.
/// </summary>
public sealed class TicketWorkflowPolicy : ITicketWorkflowPolicy
{
    private readonly IDomainConfigurationService _domainConfig;
    private readonly RuleCompilerService _compiler;
    private readonly ILogger<TicketWorkflowPolicy> _logger;

    // Cache: (DomainId, FromState, ToState, VersionId) -> Compiled Delegate
    private readonly Dictionary<(string, string, string, string), Func<Ticket, ClaimsPrincipal, bool>> _ruleCache = new();
    private readonly object _cacheLock = new();

    public TicketWorkflowPolicy(
        IDomainConfigurationService domainConfig,
        RuleCompilerService compiler,
        ILogger<TicketWorkflowPolicy> logger)
    {
        _domainConfig = domainConfig;
        _compiler = compiler;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanTransition(Ticket ticket, Status targetStatus, ITicketWorkflowContext context)
    {
        // 1. Basic state-machine transition check (domain invariant)
        if (!Ticket.IsValidTransition(ticket.TicketStatus, targetStatus))
        {
            _logger.LogWarning(
                "Invalid transition attempt from {From} to {To} for ticket {Guid}",
                ticket.TicketStatus, targetStatus, ticket.Guid);
            return false;
        }

        // 2. Domain-config driven rule check
        var domainId = ticket.DomainId ?? _domainConfig.GetDefaultDomainId();
        var currentStatus = ticket.TicketStatus.ToString();
        var targetStatusStr = targetStatus.ToString();
        var versionId = ticket.ConfigVersionId;

        // Fast path: check if transition is defined in domain config
        IEnumerable<string> validTransitions;
        if (!string.IsNullOrEmpty(versionId))
        {
            validTransitions = _domainConfig.GetValidTransitionsByVersion(domainId, currentStatus, versionId);
        }
        else
        {
            validTransitions = _domainConfig.GetValidTransitions(domainId, currentStatus);
        }

        if (!validTransitions.Contains(targetStatusStr, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Transition {From}->{To} not defined in domain {Domain} version {Version}",
                currentStatus, targetStatusStr, domainId, versionId ?? "Latest");
            return false;
        }

        // 3. Advanced compiled rule check (user-specific permissions)
        // Map ITicketWorkflowContext back to ClaimsPrincipal for rule evaluation
        var claimsPrincipal = MapToClaimsPrincipal(context);
        var ruleDelegate = GetOrCompileRule(domainId, currentStatus, targetStatusStr, versionId);
        return ruleDelegate(ticket, claimsPrincipal);
    }

    /// <inheritdoc />
    public IEnumerable<Status> GetValidNextStates(Ticket ticket, ITicketWorkflowContext context)
    {
        var domainId = ticket.DomainId ?? _domainConfig.GetDefaultDomainId();
        var currentStatus = ticket.TicketStatus.ToString();
        var versionId = ticket.ConfigVersionId;

        IEnumerable<string> validTransitions;
        if (!string.IsNullOrEmpty(versionId))
        {
            validTransitions = _domainConfig.GetValidTransitionsByVersion(domainId, currentStatus, versionId);
        }
        else
        {
            validTransitions = _domainConfig.GetValidTransitions(domainId, currentStatus);
        }

        var claimsPrincipal = MapToClaimsPrincipal(context);
        var result = new List<Status>();

        foreach (var statusStr in validTransitions)
        {
            if (Enum.TryParse<Status>(statusStr, true, out var statusEnum))
            {
                if (CanTransition(ticket, statusEnum, context))
                {
                    result.Add(statusEnum);
                }
            }
        }

        return result;
    }

    private Func<Ticket, ClaimsPrincipal, bool> GetOrCompileRule(string domainId, string from, string to, string? versionId)
    {
        var key = (domainId, from, to, versionId ?? "Latest");

        // Fast path: Check cache
        lock (_cacheLock)
        {
            if (_ruleCache.TryGetValue(key, out var cachedFunc))
            {
                return cachedFunc;
            }
        }

        // Slow path: Compile
        DomainConfig? domain;
        if (!string.IsNullOrEmpty(versionId))
        {
            domain = _domainConfig.GetDomainByVersion(domainId, versionId);
        }
        else
        {
            domain = _domainConfig.GetDomain(domainId);
        }

        var rules = domain?.Workflow.TransitionRules?
            .Where(r => r.From.Equals(from, StringComparison.OrdinalIgnoreCase) &&
                        r.To.Equals(to, StringComparison.OrdinalIgnoreCase))
            .SelectMany(r => r.Conditions)
            .ToList();

        var compiledFunc = _compiler.Compile(rules);

        lock (_cacheLock)
        {
            _ruleCache[key] = compiledFunc;
        }

        _logger.LogInformation(
            "Compiled workflow rule for {Domain} version {Version}: {From}->{To}",
            domainId, versionId ?? "Latest", from, to);

        return compiledFunc;
    }

    /// <summary>
    /// Maps a domain <see cref="ITicketWorkflowContext"/> to an ASP.NET <see cref="ClaimsPrincipal"/>
    /// for rule evaluation. This is an internal adapter; rules should migrate to
    /// <see cref="ITicketWorkflowContext"/> in the future.
    /// </summary>
    private static ClaimsPrincipal MapToClaimsPrincipal(ITicketWorkflowContext context)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, context.UserId)
        };

        foreach (var role in context.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "workflow-policy");
        return new ClaimsPrincipal(identity);
    }
}
