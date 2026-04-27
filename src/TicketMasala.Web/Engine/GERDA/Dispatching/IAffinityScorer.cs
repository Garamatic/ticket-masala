using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// Interface for affinity scoring between agents and tickets.
/// Implemented by various strategies (ML-based, heuristic, etc.)
/// </summary>
public interface IAffinityScorer
{
    /// <summary>
    /// Calculate affinity score between an agent and a ticket.
    /// </summary>
    /// <param name="employee">The agent/employee</param>
    /// <param name="ticket">The ticket to match against</param>
    /// <param name="customer">The customer who created the ticket</param>
    /// <returns>Affinity score (typically 0-5 or 0-100 depending on implementation)</returns>
    double CalculateAffinity(Employee employee, Ticket ticket, ApplicationUser? customer);

    /// <summary>
    /// Get explanation for why this score was assigned.
    /// </summary>
    string GetAffinityExplanation(double score, Employee employee, Ticket ticket);

    /// <summary>
    /// Whether this scorer has sufficient data to provide meaningful scores.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Last time the affinity model was trained/updated.
    /// </summary>
    DateTime? LastTrained { get; }

    /// <summary>
    /// Trigger retraining of the affinity model (if applicable).
    /// </summary>
    Task RetrainAsync();
}
