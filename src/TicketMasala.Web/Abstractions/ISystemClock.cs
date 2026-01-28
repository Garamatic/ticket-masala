namespace TicketMasala.Web.Abstractions;

/// <summary>
/// Abstraction for system time to enable deterministic testing.
/// Replaces direct DateTime.UtcNow calls throughout the application.
/// </summary>
public interface ISystemClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }
}
