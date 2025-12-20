using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Security.Claims;
using TicketMasala.Web.Models;
using TicketMasala.Web.Models.Configuration;

namespace TicketMasala.Web.Engine.Compiler;

/// <summary>
/// Compiles transition rules into executable delegates using Expression Trees.
/// Avoids runtime interpretation and string parsing during rule evaluation.
/// Supports atomic hot-reload of rules.
/// </summary>
public class RuleCompilerService
{
    private readonly ILogger<RuleCompilerService> _logger;

    // Cache: "Domain:State:TargetState" -> Delegate
    private ConcurrentDictionary<string, Func<Ticket, ClaimsPrincipal, bool>> _compiledRules = new();

    public RuleCompilerService(ILogger<RuleCompilerService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the compiled delegate for a specific transition rule.
    /// Returns a default "Allow All" delegate if no rule exists (fail-open or fail-closed policy can be adjusted).
    /// </summary>
    public Func<Ticket, ClaimsPrincipal, bool> GetRuleDelegate(string ruleKey)
    {
        if (_compiledRules.TryGetValue(ruleKey, out var rule))
        {
            return rule;
        }

        // Default behavior: If no rule is defined, is it allowed?
        // Assuming explicit rules are restrictions, so missing rule = allow.
        // Or if transitions are defined in workflow, they might just be allow-all unless restricted.
        return (t, u) => true;
    }

    /// <summary>
    /// Atomically replaces the entire rule cache with a new set compiled from the provided configuration.
    /// This ensures zero downtime during configuration reloads.
    /// </summary>
    public void ReplaceRuleCache(MasalaDomainsConfig newConfiguration)
    {
        _logger.LogInformation("Compiling rules for hot reload...");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var newRules = new ConcurrentDictionary<string, Func<Ticket, ClaimsPrincipal, bool>>();

        try
        {
            foreach (var domain in newConfiguration.Domains)
            {
                var domainId = domain.Key;
                var rankingConfig = domain.Value.AiStrategies.Ranking;

                // Compile Ranking Rules
                if (rankingConfig != null && rankingConfig.Multipliers != null)
                {
                    for (int i = 0; i < rankingConfig.Multipliers.Count; i++)
                    {
                        var multiplier = rankingConfig.Multipliers[i];
                        if (multiplier.Conditions != null && multiplier.Conditions.Any())
                        {
                            var ruleKey = $"ranking:{domainId}:{i}";
                            var compiled = Compile(multiplier.Conditions);
                            newRules.TryAdd(ruleKey, compiled);
                        }
                    }
                }

                // Placeholder for Transition Rules (Logic remains similar if needed later)
            }

            // ATOMIC SWAP
            // Interlocked.Exchange replaces the reference to _compiledRules with newRules.
            // Any request currently executing GetRuleDelegate might hold a reference to the OLD dictionary,
            // which is fine (GC will handle it eventually). New requests get the NEW dictionary immediately.
            Interlocked.Exchange(ref _compiledRules, newRules);

            sw.Stop();
            _logger.LogInformation("Successfully swapped {Count} compiled rule delegates in {Elapsed}ms.", newRules.Count, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recompile rules. Keeping existing rules active.");
            throw; // Propagate to caller (DomainConfigService) so it knows reload failed
        }
    }

    /// <summary>
    /// Compiles a list of conditions into a single delegate: (Ticket, User) => bool
    /// </summary>
    public Func<Ticket, ClaimsPrincipal, bool> Compile(List<TransitionCondition>? conditions)
    {
        if (conditions == null || !conditions.Any())
        {
            return (t, u) => true;
        }

        var ticketParam = Expression.Parameter(typeof(Ticket), "ticket");
        var userParam = Expression.Parameter(typeof(ClaimsPrincipal), "user");

        Expression combinedExpression = Expression.Constant(true);

        foreach (var condition in conditions)
        {
            Expression? conditionExpr = null;

            // 1. Compile Role Check
            if (!string.IsNullOrEmpty(condition.Role))
            {
                conditionExpr = BuildRoleCheck(userParam, condition.Role);
            }

            // 2. Compile Field Check
            if (!string.IsNullOrEmpty(condition.Field))
            {
                var fieldCheck = BuildFieldCheck(ticketParam, condition);
                if (conditionExpr == null)
                {
                    conditionExpr = fieldCheck;
                }
                else
                {
                    // If both Role and Field are present, AND them
                    conditionExpr = Expression.AndAlso(conditionExpr, fieldCheck);
                }
            }

            if (conditionExpr != null)
            {
                combinedExpression = Expression.AndAlso(combinedExpression, conditionExpr);
            }
        }

        try
        {
            return Expression.Lambda<Func<Ticket, ClaimsPrincipal, bool>>(
                combinedExpression, ticketParam, userParam).Compile();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compile rule set");
            return (t, u) => false; // Fail safe
        }
    }

    private Expression BuildRoleCheck(ParameterExpression userParam, string role)
    {
        // user.IsInRole("roleName")
        var method = typeof(ClaimsPrincipal).GetMethod(nameof(ClaimsPrincipal.IsInRole), new[] { typeof(string) });
        return Expression.Call(userParam, method!, Expression.Constant(role));
    }

    private Expression BuildFieldCheck(ParameterExpression ticketParam, TransitionCondition condition)
    {
        // V2: Support Virtual Fields (days_until_breach, age_days)
        if (condition.Field == "days_until_breach")
        {
            // (ticket.CompletionTarget.HasValue ? (ticket.CompletionTarget.Value - DateTime.UtcNow).TotalDays : 99999.0)

            var completionTargetProp = Expression.Property(ticketParam, nameof(Ticket.CompletionTarget));
            var hasValue = Expression.Property(completionTargetProp, nameof(Nullable<DateTime>.HasValue));
            var value = Expression.Property(completionTargetProp, nameof(Nullable<DateTime>.Value));
            var utcNow = Expression.Property(null, typeof(DateTime).GetProperty(nameof(DateTime.UtcNow))!);

            var timeSpan = Expression.Subtract(value, utcNow);
            var totalDays = Expression.Property(timeSpan, nameof(TimeSpan.TotalDays));

            var fallback = Expression.Constant(99999.0); // No deadline = not breaching

            var calculated = Expression.Condition(hasValue, totalDays, fallback);

            // Build comparison
            if (double.TryParse(condition.Value?.ToString(), out var targetVal))
            {
                var target = Expression.Constant(targetVal);
                var opVal = condition.Operator?.ToLowerInvariant() ?? "==";
                return opVal switch
                {
                    ">" => Expression.GreaterThan(calculated, target),
                    ">=" => Expression.GreaterThanOrEqual(calculated, target),
                    "<" => Expression.LessThan(calculated, target),
                    "<=" => Expression.LessThanOrEqual(calculated, target),
                    "!=" => Expression.NotEqual(calculated, target),
                    _ => Expression.Equal(calculated, target)
                };
            }
            return Expression.Constant(false);
        }

        if (condition.Field == "age_days")
        {
            // (DateTime.UtcNow - ticket.CreationDate).TotalDays
            var creationDateProp = Expression.Property(ticketParam, nameof(Ticket.CreationDate));
            var utcNow = Expression.Property(null, typeof(DateTime).GetProperty(nameof(DateTime.UtcNow))!);
            var timeSpan = Expression.Subtract(utcNow, creationDateProp);
            var totalDays = Expression.Property(timeSpan, nameof(TimeSpan.TotalDays));

            if (double.TryParse(condition.Value?.ToString(), out var targetVal))
            {
                var target = Expression.Constant(targetVal);
                var opAge = condition.Operator?.ToLowerInvariant() ?? "==";
                return opAge switch
                {
                    ">" => Expression.GreaterThan(totalDays, target),
                    ">=" => Expression.GreaterThanOrEqual(totalDays, target),
                    "<" => Expression.LessThan(totalDays, target),
                    "<=" => Expression.LessThanOrEqual(totalDays, target),
                    "!=" => Expression.NotEqual(totalDays, target),
                    _ => Expression.Equal(totalDays, target)
                };
            }
            return Expression.Constant(false);
        }

        // FieldExtractor.GetString(ticket.CustomFieldsJson, "fieldName") -> Value
        var jsonProp = Expression.Property(ticketParam, nameof(Ticket.CustomFieldsJson));
        var keyConst = Expression.Constant(condition.Field);

        // Determine type and method based on operator/value
        // Logic: If operator implies numeric comparison, use GetNumber. 
        // If boolean, use GetBool. Default String.

        // Simplified inference for this implementation:
        // If Value parses as double, use number comparison.
        // If Value is "true"/"false", use bool.
        // Else string.
        // Special case: is_empty / is_not_empty checks string null/empty.

        string op = condition.Operator?.ToLowerInvariant() ?? "==";

        if (op == "is_empty")
        {
            var call = Expression.Call(typeof(FieldExtractor).GetMethod(nameof(FieldExtractor.GetString))!, jsonProp, keyConst);
            // string.IsNullOrEmpty(call)
            return Expression.Call(typeof(string).GetMethod(nameof(string.IsNullOrEmpty))!, call);
        }

        if (op == "is_not_empty")
        {
            var call = Expression.Call(typeof(FieldExtractor).GetMethod(nameof(FieldExtractor.GetString))!, jsonProp, keyConst);
            // !string.IsNullOrEmpty(call)
            return Expression.Not(Expression.Call(typeof(string).GetMethod(nameof(string.IsNullOrEmpty))!, call));
        }

        // Numeric Comparison
        if (double.TryParse(condition.Value?.ToString(), out var numVal))
        {
            var method = typeof(FieldExtractor).GetMethod(nameof(FieldExtractor.GetNumber), new[] { typeof(string), typeof(string) });
            var call = Expression.Call(method!, jsonProp, keyConst);
            var target = Expression.Constant(numVal);

            return op switch
            {
                ">" => Expression.GreaterThan(call, target),
                ">=" => Expression.GreaterThanOrEqual(call, target),
                "<" => Expression.LessThan(call, target),
                "<=" => Expression.LessThanOrEqual(call, target),
                "!=" => Expression.NotEqual(call, target),
                _ => Expression.Equal(call, target) // Default ==
            };
        }

        // Boolean Comparison
        if (bool.TryParse(condition.Value?.ToString(), out var boolVal))
        {
            var method = typeof(FieldExtractor).GetMethod(nameof(FieldExtractor.GetBool), new[] { typeof(string), typeof(string) });
            var call = Expression.Call(method!, jsonProp, keyConst);
            var target = Expression.Constant(boolVal);
            return Expression.Equal(call, target);
        }

        // String Comparison
        var strMethod = typeof(FieldExtractor).GetMethod(nameof(FieldExtractor.GetString), new[] { typeof(string), typeof(string) });
        var strCall = Expression.Call(strMethod!, jsonProp, keyConst);
        var strTarget = Expression.Constant(condition.Value?.ToString() ?? "");

        // String.Equals(a, b, StringComparison.OrdinalIgnoreCase)
        var equalsMethod = typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string), typeof(string), typeof(StringComparison) });
        var comparison = Expression.Call(equalsMethod!, strCall, strTarget, Expression.Constant(StringComparison.OrdinalIgnoreCase));

        return op == "!=" ? Expression.Not(comparison) : comparison;
    }
}
