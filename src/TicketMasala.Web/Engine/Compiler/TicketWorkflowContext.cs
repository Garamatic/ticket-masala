using System.Security.Claims;
using TicketMasala.Domain.Workflow;

namespace TicketMasala.Web.Engine.Compiler;

/// <summary>
/// Adapter from ASP.NET Core <see cref="ClaimsPrincipal"/> to domain <see cref="ITicketWorkflowContext"/>.
/// </summary>
public sealed class TicketWorkflowContext : ITicketWorkflowContext
{
    public string UserId { get; }
    public IReadOnlyList<string> Roles { get; }

    public TicketWorkflowContext(string userId, IReadOnlyList<string> roles)
    {
        UserId = userId;
        Roles = roles;
    }

    public bool HasRole(string role) => Roles.Contains(role);

    /// <summary>
    /// Creates a <see cref="TicketWorkflowContext"/> from an ASP.NET <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public static TicketWorkflowContext FromClaimsPrincipal(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return new TicketWorkflowContext(userId, roles);
    }
}
