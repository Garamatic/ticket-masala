using IT_Project2526.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Repositories
{
    /// <summary>
    /// Repository implementation for Ticket-specific operations
    /// </summary>
    public class TicketRepository : Repository<Ticket>, ITicketRepository
    {
        public TicketRepository(ITProjectDB context, ILogger<Repository<Ticket>> logger) 
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Ticket>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(t => t.Responsible)
                .Include(t => t.Customer)
                .Include(t => t.Watchers)
                .Include(t => t.ParentTicket)
                .Include(t => t.SubTickets)
                .Where(t => t.ValidUntil == null)
                .OrderByDescending(t => t.CreationDate)
                .ToListAsync();
        }

        public async Task<Ticket?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(t => t.Responsible)
                .Include(t => t.Customer)
                .Include(t => t.Watchers)
                .Include(t => t.ParentTicket)
                .Include(t => t.SubTickets)
                    .ThenInclude(st => st.Responsible)
                .FirstOrDefaultAsync(t => t.Guid == id && t.ValidUntil == null);
        }

        public async Task<IEnumerable<Ticket>> GetByProjectIdAsync(Guid projectId)
        {
            return await _context.Projects
                .Where(p => p.Guid == projectId)
                .SelectMany(p => p.Tasks)
                .Include(t => t.Responsible)
                .Include(t => t.Customer)
                .Where(t => t.ValidUntil == null)
                .OrderByDescending(t => t.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetByCustomerIdAsync(string customerId)
        {
            return await _dbSet
                .Include(t => t.Responsible)
                .Include(t => t.ParentTicket)
                .Where(t => t.Customer.Id == customerId && t.ValidUntil == null)
                .OrderByDescending(t => t.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetByResponsibleIdAsync(string userId)
        {
            return await _dbSet
                .Include(t => t.Customer)
                .Include(t => t.ParentTicket)
                .Include(t => t.Watchers)
                .Where(t => t.Responsible != null && t.Responsible.Id == userId && t.ValidUntil == null)
                .OrderByDescending(t => t.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetByStatusAsync(Status status)
        {
            return await _dbSet
                .Include(t => t.Responsible)
                .Include(t => t.Customer)
                .Where(t => t.TicketStatus == status && t.ValidUntil == null)
                .OrderByDescending(t => t.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetByTypeAsync(TicketType type)
        {
            return await _dbSet
                .Include(t => t.Responsible)
                .Include(t => t.Customer)
                .Where(t => t.TicketType == type && t.ValidUntil == null)
                .OrderByDescending(t => t.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetOverdueTicketsAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Include(t => t.Responsible)
                .Include(t => t.Customer)
                .Where(t => t.CompletionTarget.HasValue 
                         && t.CompletionTarget.Value < now
                         && t.TicketStatus != Status.Completed
                         && t.TicketStatus != Status.Failed
                         && t.ValidUntil == null)
                .OrderBy(t => t.CompletionTarget)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetWatchedByUserAsync(string userId)
        {
            return await _dbSet
                .Include(t => t.Responsible)
                .Include(t => t.Customer)
                .Include(t => t.Watchers)
                .Where(t => t.Watchers.Any(w => w.Id == userId) && t.ValidUntil == null)
                .OrderByDescending(t => t.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetSubTicketsAsync(Guid parentTicketId)
        {
            return await _dbSet
                .Include(t => t.Responsible)
                .Include(t => t.Customer)
                .Where(t => t.ParentTicket != null 
                         && t.ParentTicket.Guid == parentTicketId 
                         && t.ValidUntil == null)
                .OrderBy(t => t.CreationDate)
                .ToListAsync();
        }
    }
}
