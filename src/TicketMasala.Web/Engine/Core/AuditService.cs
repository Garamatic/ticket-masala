using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Data;

namespace TicketMasala.Web.Engine.Core;

public class AuditService : IAuditService
{
    private readonly MasalaDbContext _context;
    private readonly ISystemClock _clock;
    private readonly ILogger<AuditService> _logger;

    public AuditService(MasalaDbContext context, ISystemClock clock, ILogger<AuditService> logger)
    {
        _context = context;
        _clock = clock;
        _logger = logger;
    }

    public Task LogActionAsync(Guid ticketId, string action, string? userId, string? propertyName = null, string? oldValue = null, string? newValue = null)
    {
        try
        {
            var entry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                Action = action,
                UserId = userId,
                PropertyName = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                Timestamp = _clock.UtcNow
            };

            _context.AuditLogs.Add(entry);
            // Note: Changes are NOT committed here. They will be committed when IUnitOfWork.CommitAsync() is called.
            // This ensures audit logs are part of the same transaction as the main operation.
            _logger.LogDebug("Audit log queued for ticket {TicketId}: {Action}", ticketId, action);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - audit logging should not fail the main operation
            _logger.LogError(ex, "Failed to queue audit entry for ticket {TicketId}", ticketId);
            return Task.CompletedTask;
        }
    }

    public async Task<List<AuditLogEntry>> GetAuditLogForTicketAsync(Guid ticketId)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.TicketId == ticketId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }
}
