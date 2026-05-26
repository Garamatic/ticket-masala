using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service responsible for ticket time logging operations.
/// </summary>
[Obsolete("Use ITicketLifecycle and command records instead. This interface will be removed in a future release.", false)]
public interface ITicketTimeLoggingService
{
    /// <summary>
    /// Logs time worked on a ticket.
    /// </summary>
    /// <param name="ticketId">The ticket</param>
    /// <param name="userId">The user logging time</param>
    /// <param name="hours">Hours worked</param>
    /// <param name="date">Date of work</param>
    /// <param name="description">Description of work performed</param>
    /// <returns>The created time log entry</returns>
    /// <exception cref="ArgumentException">Thrown if ticket not found or hours invalid</exception>
    Task<TimeLog> LogTimeAsync(Guid ticketId, string userId, double hours, DateTime date, string description);
}

/// <summary>
/// Implementation of ticket time logging operations.
/// </summary>
internal class TicketTimeLoggingService : ITicketTimeLoggingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ISystemClock _clock;
    private readonly ILogger<TicketTimeLoggingService> _logger;

    public TicketTimeLoggingService(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ISystemClock clock,
        ILogger<TicketTimeLoggingService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TimeLog> LogTimeAsync(Guid ticketId, string userId, double hours, DateTime date, string description)
    {
        // Validate ticket exists
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId, includeRelations: false);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found", nameof(ticketId));
        }

        // Validate hours
        if (hours <= 0)
        {
            throw new ArgumentException("Hours must be greater than zero", nameof(hours));
        }

        if (hours > 24)
        {
            throw new ArgumentException("Hours cannot exceed 24 in a single entry", nameof(hours));
        }

        var timeLog = new TimeLog
        {
            TicketId = ticketId,
            UserId = userId,
            Hours = hours,
            Date = date,
            Description = description,
            CreationDate = _clock.UtcNow
        };

        await _unitOfWork.AddTimeLogAsync(timeLog);
        await _unitOfWork.CommitAsync();

        await _auditService.LogActionAsync(
            ticketId,
            "TimeLogged",
            userId,
            "TimeLog",
            null,
            $"{hours} hours");

        _logger.LogInformation(
            "Time logged for ticket {TicketId}: {Hours} hours by {UserId} on {Date}",
            ticketId,
            hours,
            userId,
            date.ToString("yyyy-MM-dd"));

        return timeLog;
    }
}
