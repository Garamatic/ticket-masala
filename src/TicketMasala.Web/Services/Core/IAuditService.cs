using TicketMasala.Web.Models;

namespace TicketMasala.Web.Services.Core;
    public interface IAuditService
    {
        Task LogActionAsync(Guid ticketId, string action, string? userId, string? propertyName = null, string? oldValue = null, string? newValue = null);
        Task<List<AuditLogEntry>> GetAuditLogForTicketAsync(Guid ticketId);
}
