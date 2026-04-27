namespace TicketMasala.Web.Engine.GERDA.Dispatching.Models;

/// <summary>
/// Generic work item interface for both Tickets and TaxCases.
/// Allows dispatch algorithms to work with any domain entity.
/// </summary>
public interface IWorkItem
{
    /// <summary>Unique identifier</summary>
    string Id { get; }

    /// <summary>Type of work (e.g., "Ticket", "TaxCase", "Hotel Tax")</summary>
    string WorkType { get; }

    /// <summary>Financial impact or value</summary>
    decimal FinancialValue { get; }

    /// <summary>Risk score (0-100)</summary>
    decimal RiskScore { get; }

    /// <summary>When was this created</summary>
    DateTime CreatedAt { get; }

    /// <summary>Optional: Completion target/deadline (if applicable)</summary>
    DateTime? TargetCompletionDate { get; }

    /// <summary>Optional: Estimated effort/job size in story points</summary>
    int? EstimatedJobSize { get; }

    /// <summary>Optional: Custom metadata payload (JSON string)</summary>
    string? MetadataJson { get; }
}
