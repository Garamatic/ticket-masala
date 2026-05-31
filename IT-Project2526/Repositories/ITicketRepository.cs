using IT_Project2526.Models;
using IT_Project2526.ViewModels;

namespace IT_Project2526.Repositories;

/// <summary>
/// Repository interface for Ticket entity operations.
/// Implements Repository pattern to abstract data access from business logic.
/// Enables unit testing and swappable data layers (EF Core, Dapper, etc.)
/// </summary>
public interface ITicketRepository
{
    // Read operations
    Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<IEnumerable<Ticket>> GetAllAsync(Guid? departmentId = null);
    Task<IEnumerable<Ticket>> GetUnassignedAsync(Guid? departmentId = null);
    Task<IEnumerable<Ticket>> GetByStatusAsync(Status status, Guid? departmentId = null);
    Task<IEnumerable<Ticket>> GetByCustomerIdAsync(string customerId);
    Task<IEnumerable<Ticket>> GetByResponsibleIdAsync(string responsibleId);
    Task<IEnumerable<Ticket>> GetByProjectGuidAsync(Guid projectGuid);
    Task<IEnumerable<Ticket>> GetRecentAsync(int timeWindowMinutes, Guid? departmentId = null);
    Task<IEnumerable<Ticket>> GetPendingOrAssignedAsync(Guid? departmentId = null);
    Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel, Guid? departmentId = null);
    
    // Write operations
    Task<Ticket> AddAsync(Ticket ticket);
    Task UpdateAsync(Ticket ticket);
    Task DeleteAsync(Guid id);
    
    // Bulk operations
    Task<IEnumerable<Ticket>> GetActiveTicketsAsync();
    Task<IEnumerable<Ticket>> GetCompletedTicketsAsync();
    Task<int> CountAsync();
    Task<bool> ExistsAsync(Guid id);
}
