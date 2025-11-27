using IT_Project2526.Models;

namespace IT_Project2526.Repositories
{
    /// <summary>
    /// Repository interface for Ticket-specific operations
    /// </summary>
    public interface ITicketRepository : IRepository<Ticket>
    {
        /// <summary>
        /// Gets all tickets with related data
        /// </summary>
        Task<IEnumerable<Ticket>> GetAllWithDetailsAsync();
        
        /// <summary>
        /// Gets a ticket by ID with all related data
        /// </summary>
        Task<Ticket?> GetByIdWithDetailsAsync(Guid id);
        
        /// <summary>
        /// Gets all tickets for a specific project
        /// </summary>
        Task<IEnumerable<Ticket>> GetByProjectIdAsync(Guid projectId);
        
        /// <summary>
        /// Gets all tickets for a specific customer
        /// </summary>
        Task<IEnumerable<Ticket>> GetByCustomerIdAsync(string customerId);
        
        /// <summary>
        /// Gets all tickets assigned to a specific user
        /// </summary>
        Task<IEnumerable<Ticket>> GetByResponsibleIdAsync(string userId);
        
        /// <summary>
        /// Gets tickets by status
        /// </summary>
        Task<IEnumerable<Ticket>> GetByStatusAsync(Status status);
        
        /// <summary>
        /// Gets tickets by type
        /// </summary>
        Task<IEnumerable<Ticket>> GetByTypeAsync(TicketType type);
        
        /// <summary>
        /// Gets overdue tickets (completion target passed and not completed)
        /// </summary>
        Task<IEnumerable<Ticket>> GetOverdueTicketsAsync();
        
        /// <summary>
        /// Gets tickets watched by a specific user
        /// </summary>
        Task<IEnumerable<Ticket>> GetWatchedByUserAsync(string userId);
        
        /// <summary>
        /// Gets child tickets of a parent ticket
        /// </summary>
        Task<IEnumerable<Ticket>> GetSubTicketsAsync(Guid parentTicketId);
    }
}
