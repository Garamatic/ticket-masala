using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// No-op affinity scorer that returns neutral scores.
/// Used when ML model is not available (e.g., in tests or fresh deployments).
/// </summary>
public sealed class NoOpAffinityScorer : IAffinityScorer
{
    public bool IsReady => false;
    public DateTime? LastTrained => null;

    public double CalculateAffinity(Employee employee, Ticket ticket, ApplicationUser? customer)
    {
        // Return neutral score - doesn't influence ranking positively or negatively
        return 0.5;
    }

    public string GetAffinityExplanation(double score, Employee employee, Ticket ticket)
    {
        return "No affinity data available (model not trained).";
    }

    public Task RetrainAsync()
    {
        // No-op: nothing to retrain
        return Task.CompletedTask;
    }
}
