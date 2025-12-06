using IT_Project2526.Models;

namespace IT_Project2526.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(Guid ticketId, string action, string? userId, string? propertyName = null, string? oldValue = null, string? newValue = null);
        Task<List<AuditLogEntry>> GetAuditLogForTicketAsync(Guid ticketId);
    }
}
