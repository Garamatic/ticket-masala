using TicketMasala.Web.Abstractions;

namespace TicketMasala.Web.Services;

/// <summary>
/// Production implementation of ISystemClock that returns actual system time.
/// </summary>
public class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
