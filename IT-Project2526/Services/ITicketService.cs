using IT_Project2526.Models;
using IT_Project2526.ViewModels;

namespace IT_Project2526.Services
{
    /// <summary>
    /// Service interface for ticket business logic
    /// </summary>
    public interface ITicketService
    {
        /// <summary>
        /// Gets all tickets
        /// </summary>
        Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync();
        
        /// <summary>
        /// Gets a ticket by ID with details
        /// </summary>
        Task<TicketViewModel?> GetTicketByIdAsync(Guid id);
        
        /// <summary>
        /// Creates a new ticket
        /// </summary>
        Task<Guid> CreateTicketAsync(TicketViewModel model, string currentUserId);
        
        /// <summary>
        /// Updates an existing ticket
        /// </summary>
        Task UpdateTicketAsync(Guid id, TicketViewModel model);
        
        /// <summary>
        /// Deletes (soft delete) a ticket
        /// </summary>
        Task DeleteTicketAsync(Guid id);
        
        /// <summary>
        /// Gets tickets for a specific project
        /// </summary>
        Task<IEnumerable<TicketViewModel>> GetProjectTicketsAsync(Guid projectId);
        
        /// <summary>
        /// Gets tickets for a specific customer
        /// </summary>
        Task<IEnumerable<TicketViewModel>> GetCustomerTicketsAsync(string customerId);
        
        /// <summary>
        /// Gets tickets assigned to a user
        /// </summary>
        Task<IEnumerable<TicketViewModel>> GetUserTicketsAsync(string userId);
        
        /// <summary>
        /// Gets tickets watched by a user
        /// </summary>
        Task<IEnumerable<TicketViewModel>> GetWatchedTicketsAsync(string userId);
        
        /// <summary>
        /// Gets overdue tickets
        /// </summary>
        Task<IEnumerable<TicketViewModel>> GetOverdueTicketsAsync();
        
        /// <summary>
        /// Assigns a ticket to a user
        /// </summary>
        Task AssignTicketAsync(Guid ticketId, string userId);
        
        /// <summary>
        /// Updates ticket status
        /// </summary>
        Task UpdateTicketStatusAsync(Guid ticketId, Status newStatus);
        
        /// <summary>
        /// Adds a comment to a ticket
        /// </summary>
        Task AddCommentAsync(Guid ticketId, string comment, string userId);
        
        /// <summary>
        /// Adds a watcher to a ticket
        /// </summary>
        Task AddWatcherAsync(Guid ticketId, string userId);
        
        /// <summary>
        /// Removes a watcher from a ticket
        /// </summary>
        Task RemoveWatcherAsync(Guid ticketId, string userId);
    }
}
