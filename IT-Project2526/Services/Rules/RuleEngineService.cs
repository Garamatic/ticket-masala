using System.Security.Claims;
using System.Text.Json;
using IT_Project2526.Models;
using IT_Project2526.Models.Configuration;
using IT_Project2526.Services.Configuration;

namespace IT_Project2526.Services.Rules
{
    public class RuleEngineService : IRuleEngineService
    {
        private readonly IDomainConfigurationService _domainConfig;
        private readonly RuleCompilerService _compiler;
        private readonly ILogger<RuleEngineService> _logger;
        
        // Cache: (DomainId, FromState, ToState) -> Compiled Delegate
        private readonly Dictionary<(string, string, string), Func<Ticket, ClaimsPrincipal, bool>> _ruleCache = new();
        private readonly object _cacheLock = new();

        public RuleEngineService(
            IDomainConfigurationService domainConfig,
            RuleCompilerService compiler,
            ILogger<RuleEngineService> logger)
        {
            _domainConfig = domainConfig;
            _compiler = compiler;
            _logger = logger;
        }

        public bool CanTransition(Ticket ticket, Status targetStatus, ClaimsPrincipal user)
        {
            var domainId = ticket.DomainId ?? _domainConfig.GetDefaultDomainId();
            var currentStatus = ticket.TicketStatus.ToString();
            var targetStatusStr = targetStatus.ToString();

            // 1. Basic Workflow Transition Check
            var validTransitions = _domainConfig.GetValidTransitions(domainId, currentStatus);
            if (!validTransitions.Contains(targetStatusStr, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid transition attempt from {From} to {To} for ticket {Guid}", currentStatus, targetStatusStr, ticket.Guid);
                return false;
            }

            // 2. Advanced Rule Check utilizing Compiled Delegates
            var ruleDelegate = GetOrCompileRule(domainId, currentStatus, targetStatusStr);
            return ruleDelegate(ticket, user);
        }

        private Func<Ticket, ClaimsPrincipal, bool> GetOrCompileRule(string domainId, string from, string to)
        {
            var key = (domainId, from, to);
            
            // Fast path: Check cache
            lock (_cacheLock)
            {
                if (_ruleCache.TryGetValue(key, out var cachedFunc))
                {
                    return cachedFunc;
                }
            }

            // Slow path: Compile
            var domain = _domainConfig.GetDomain(domainId);
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

            _logger.LogInformation("Compiled access rule for {Domain}: {From}->{To}", domainId, from, to);
            return compiledFunc;
        }

        public IEnumerable<Status> GetValidNextStates(Ticket ticket, ClaimsPrincipal user)
        {
            var domainId = ticket.DomainId ?? _domainConfig.GetDefaultDomainId();
            var currentStatus = ticket.TicketStatus.ToString();
            var validTransitions = _domainConfig.GetValidTransitions(domainId, currentStatus);
            
            var validStates = new List<Status>();

            foreach (var statusStr in validTransitions)
            {
                if (Enum.TryParse<Status>(statusStr, true, out var statusEnum))
                {
                    if (CanTransition(ticket, statusEnum, user))
                    {
                        validStates.Add(statusEnum);
                    }
                }
            }

            return validStates;
        }

        public IEnumerable<string> ValidateRequiredFields(Ticket ticket, Status targetStatus)
        {
            // Placeholder: Future compiled check for required fields
            return Enumerable.Empty<string>();
        }
    }
}
