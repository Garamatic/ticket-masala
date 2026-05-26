using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

/// <summary>
/// Structured result of a ticket lifecycle command.
/// Never throws on domain validation — all failures are captured here.
/// </summary>
public sealed record TicketResult
{
    public bool Success { get; init; }
    public Ticket? Ticket { get; init; }
    public TicketComment? Comment { get; init; }
    public TimeLog? TimeLog { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public Guid? AuditLogId { get; init; }

    public static TicketResult Ok(Ticket ticket, string? warning = null)
    {
        var warnings = string.IsNullOrEmpty(warning)
            ? Array.Empty<string>()
            : new[] { warning };
        return new TicketResult { Success = true, Ticket = ticket, Warnings = warnings };
    }

    public static TicketResult Ok(TicketComment comment)
    {
        return new TicketResult { Success = true, Comment = comment };
    }

    public static TicketResult Ok(TimeLog timeLog)
    {
        return new TicketResult { Success = true, TimeLog = timeLog };
    }

    public static TicketResult Fail(string message)
    {
        return new TicketResult { Success = false, ErrorMessage = message };
    }

    public static TicketResult Fail(Exception ex)
    {
        return new TicketResult { Success = false, ErrorMessage = ex.Message };
    }
}
