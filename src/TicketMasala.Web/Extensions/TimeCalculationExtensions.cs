using TicketMasala.Web.Abstractions;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Migration helper for view models and calculated properties that depend on current time.
/// Gradually migrate DateTime.UtcNow direct usage to injected ISystemClock dependency.
/// </summary>
public static class TimeCalculationExtensions
{
    /// <summary>
    /// Calculates days until SLA deadline using injected clock (testable).
    /// </summary>
    public static int DaysUntilSla(this DateTime? completionTarget, ISystemClock clock)
    {
        return completionTarget.HasValue
            ? (int)(completionTarget.Value - clock.UtcNow).TotalDays
            : int.MaxValue;
    }

    /// <summary>
    /// Checks if SLA has been breached using injected clock (testable).
    /// </summary>
    public static bool IsSlaBreached(this DateTime? completionTarget, ISystemClock clock)
    {
        return completionTarget.HasValue && clock.UtcNow > completionTarget.Value;
    }

    /// <summary>
    /// Calculates time spent in backlog using injected clock (testable).
    /// </summary>
    public static TimeSpan TimeInBacklog(this DateTime creationDate, ISystemClock clock)
    {
        return clock.UtcNow - creationDate;
    }

    /// <summary>
    /// Gets hours since ticket creation using injected clock (testable).
    /// </summary>
    public static double HoursSinceCreation(this DateTime creationDate, ISystemClock clock)
    {
        return (clock.UtcNow - creationDate).TotalHours;
    }

    /// <summary>
    /// Checks if ticket is overdue using injected clock (testable).
    /// </summary>
    public static bool IsOverdue(this DateTime dueDate, ISystemClock clock)
    {
        return clock.UtcNow > dueDate;
    }

    /// <summary>
    /// Gets remaining time until deadline using injected clock (testable).
    /// </summary>
    public static TimeSpan RemainingTime(this DateTime? dueDate, ISystemClock clock)
    {
        return dueDate.HasValue ? dueDate.Value - clock.UtcNow : TimeSpan.MaxValue;
    }
}
