namespace TicketMasala.Web.Engine.Core;

using TicketMasala.Web.Models;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Data;

public class AuditService : IAuditService
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(MasalaDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActionAsync(Guid ticketId, string action, string? userId, string? propertyName = null, string? oldValue = null, string? newValue = null)
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
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(entry);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry for ticket {TicketId}", ticketId);
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
