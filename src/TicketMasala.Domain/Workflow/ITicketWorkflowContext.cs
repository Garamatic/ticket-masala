namespace TicketMasala.Domain.Workflow;

/// <summary>
/// Domain-level abstraction for user identity during workflow operations.
/// Hides ASP.NET Core ClaimsPrincipal from the domain layer.
/// </summary>
public interface ITicketWorkflowContext
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// The roles the user possesses.
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Whether the user has a specific role.
    /// </summary>
    bool HasRole(string role);
}
