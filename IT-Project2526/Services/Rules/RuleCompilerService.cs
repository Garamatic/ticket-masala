using System.Linq.Expressions;
using System.Security.Claims;
using IT_Project2526.Models;
using IT_Project2526.Models.Configuration;

namespace IT_Project2526.Services.Rules;

/// <summary>
/// Compiles transition rules into executable delegates using Expression Trees.
/// Avoids runtime interpretation and string parsing during rule evaluation.
/// </summary>
public class RuleCompilerService
{
    private readonly ILogger<RuleCompilerService> _logger;

    public RuleCompilerService(ILogger<RuleCompilerService> logger)
    {
        _logger = logger;
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
