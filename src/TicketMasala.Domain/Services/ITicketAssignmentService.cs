using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Services;

/// <summary>
/// Domain service for complex ticket assignment operations.
/// Handles cross-aggregate concerns involving tickets, employees, and projects.
/// </summary>
public interface ITicketAssignmentService
{
    /// <summary>
    /// Assigns a ticket to an employee with full domain validation.
    /// </summary>
    /// <param name="ticket">The ticket to assign</param>
    /// <param name="employee">The employee to assign to</param>
    /// <param name="assignedByUserId">The user performing the assignment</param>
    /// <param name="assignedByRoles">Roles of the user performing assignment</param>
    /// <returns>True if assignment was successful</returns>
    /// <exception cref="DomainRuleException">Thrown when assignment violates domain rules</exception>
    Task<bool> AssignToEmployeeAsync(
        Ticket ticket,
        Employee employee,
        string assignedByUserId,
        IEnumerable<string> assignedByRoles);

    /// <summary>
    /// Unassigns a ticket from its current employee.
    /// </summary>
    Task<bool> UnassignAsync(
        Ticket ticket,
        string unassignedByUserId,
        IEnumerable<string> unassignedByRoles);

    /// <summary>
    /// Determines if automatic dispatch (AI assignment) should be attempted.
    /// </summary>
    bool ShouldAutoDispatch(Ticket ticket);

    /// <summary>
    /// Gets assignment recommendations based on ticket characteristics.
    /// This is a pure function that doesn't modify state.
    /// </summary>
    Task<AssignmentRecommendation> GetRecommendationsAsync(Ticket ticket);
}

/// <summary>
/// Result of an assignment recommendation query.
/// </summary>
public class AssignmentRecommendation
{
    /// <summary>
    /// The recommended employee ID, if any.
    /// </summary>
    public string? RecommendedEmployeeId { get; set; }

    /// <summary>
    /// The recommended project ID, if any.
    /// </summary>
    public Guid? RecommendedProjectGuid { get; set; }

    /// <summary>
    /// Confidence score (0-100) for the recommendation.
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Human-readable explanation for the recommendation.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Alternative recommendations if the primary is not suitable.
    /// </summary>
    public List<AlternativeAssignment> Alternatives { get; set; } = new();
}

/// <summary>
/// Alternative assignment option.
/// </summary>
public class AlternativeAssignment
{
    public string? EmployeeId { get; set; }
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
}
