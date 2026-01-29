using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Dispatching.Models;
using System.Text.Json;

namespace TicketMasala.Web.Engine.Common;

/// <summary>
/// Adapter that converts TicketMasala Ticket domain model to IWorkItem interface.
/// Enables generic WSJF and Agent Matching algorithms to work with TicketMasala Tickets.
/// </summary>
public class TicketWorkItemAdapter : IWorkItem
{
    private readonly Ticket _ticket;

    public TicketWorkItemAdapter(Ticket ticket)
    {
        _ticket = ticket ?? throw new ArgumentNullException(nameof(ticket));
    }

    /// <summary>Ticket unique identifier</summary>
    public string Id => _ticket.Guid.ToString();

    /// <summary>Work type from ticket type</summary>
    public string WorkType => _ticket.TicketType?.ToString() ?? "General";

    /// <summary>
    /// Financial value extracted from custom fields or estimated effort normalized.
    /// For TicketMasala, we use estimated effort points converted to notional value.
    /// Formula: EffortPoints * €1000 per point for normalization
    /// </summary>
    public decimal FinancialValue => _ticket.EstimatedEffortPoints > 0 
        ? _ticket.EstimatedEffortPoints * 1000m 
        : 5000m; // Default: €5,000

    /// <summary>
    /// Risk score: 0-100 where 100 is highest risk.
    /// For TicketMasala, computed from ticket age and SLA breach likelihood.
    /// Formula: Min(100, (DaysOld * 5) + (DaysUntilBreachOverdue * 20))
    /// </summary>
    public decimal RiskScore
    {
        get
        {
            var now = DateTime.UtcNow;
            var ageInDays = (now - _ticket.CreationDate).TotalDays;
            decimal score = (decimal)(ageInDays * 5); // Older = more risk

            // If ticket has SLA and is nearing/past deadline
            if (_ticket.CompletionTarget.HasValue)
            {
                var daysUntilTarget = (_ticket.CompletionTarget.Value - now).TotalDays;
                if (daysUntilTarget < 0)
                {
                    score += 50; // Overdue penalty
                }
                else if (daysUntilTarget < 3)
                {
                    score += 30; // Near-breach penalty
                }
            }

            return Math.Min(100, score);
        }
    }

    /// <summary>When the ticket was created</summary>
    public DateTime CreatedAt => _ticket.CreationDate;

    /// <summary>Target completion date (SLA deadline)</summary>
    public DateTime? TargetCompletionDate => _ticket.CompletionTarget;

    /// <summary>
    /// Estimated job size in Fibonacci story points (1-13).
    /// Extracted from EstimatedEffortPoints with intelligent bucketing:
    /// - 1-3 points = 1 (tiny)
    /// - 4-6 points = 3 (small)
    /// - 7-11 points = 8 (medium)
    /// - 12+ points = 13 (large)
    /// </summary>
    public int? EstimatedJobSize => EstimateJobSize(_ticket.EstimatedEffortPoints);

    /// <summary>Additional metadata as JSON string</summary>
    public string MetadataJson
    {
        get
        {
            var metadata = new
            {
                TicketId = _ticket.Guid,
                Title = _ticket.Title,
                Domain = _ticket.DomainId,
                Status = _ticket.Status,
                Priority = _ticket.PriorityScore,
                DomainCustomFields = _ticket.DomainCustomFieldsJson
            };
            return JsonSerializer.Serialize(metadata);
        }
    }

    /// <summary>
    /// Intelligently estimates job size from effort points using Fibonacci scale.
    /// </summary>
    private static int EstimateJobSize(int effortPoints)
    {
        if (effortPoints <= 0) return 5; // Default to medium

        return effortPoints switch
        {
            1 or 2 or 3 => 1,           // Tiny (1-3 pts)
            4 or 5 or 6 => 3,           // Small (4-6 pts)
            7 or 8 or 9 or 10 or 11 => 8, // Medium (7-11 pts)
            _ => 13                     // Large (12+ pts)
        };
    }
}
