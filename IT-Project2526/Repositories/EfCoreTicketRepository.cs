using IT_Project2526.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Repositories;

/// <summary>
/// EF Core implementation of ITicketRepository.
/// Adapter pattern - adapts EF Core DbContext to domain repository interface.
/// </summary>
public class EfCoreTicketRepository : ITicketRepository
{
    private readonly ITProjectDB _context;
    private readonly ILogger<EfCoreTicketRepository> _logger;

    public EfCoreTicketRepository(ITProjectDB context, ILogger<EfCoreTicketRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations = true)
    {
        var query = _context.Tickets.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(t => t.Customer)
                .Include(t => t.Responsible)
                .Include(t => t.ParentTicket)
                .Include(t => t.SubTickets)
                .Include(t => t.Project);
        }

        return await query.FirstOrDefaultAsync(t => t.Guid == id);
    }

    public async Task<IEnumerable<Ticket>> GetAllAsync()
    {
        return await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetUnassignedAsync()
    {
        return await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Project)
            .Where(t => t.ValidUntil == null)
            .Where(t => t.TicketStatus == Status.Pending || 
                       (t.TicketStatus == Status.Assigned && t.ResponsibleId == null))
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByStatusAsync(Status status)
    {
        return await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .Where(t => t.TicketStatus == status && t.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByCustomerIdAsync(string customerId)
    {
        return await _context.Tickets
            .Include(t => t.Responsible)
            .Where(t => t.CustomerId == customerId)
            .Where(t => t.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByResponsibleIdAsync(string responsibleId)
    {
        return await _context.Tickets
            .Include(t => t.Customer)
            .Where(t => t.ResponsibleId == responsibleId)
            .Where(t => t.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByProjectGuidAsync(Guid projectGuid)
    {
        return await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .Where(t => t.ProjectGuid == projectGuid)
            .Where(t => t.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetRecentAsync(int timeWindowMinutes)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);
        return await _context.Tickets
            .Where(t => t.CreationDate >= cutoffTime)
            .Where(t => t.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetPendingOrAssignedAsync()
    {
        return await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .Where(t => t.TicketStatus == Status.Pending || t.TicketStatus == Status.Assigned)
            .Where(t => t.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<Ticket> AddAsync(Ticket ticket)
    {
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Ticket {TicketGuid} added to repository", ticket.Guid);
        return ticket;
    }

    public async Task UpdateAsync(Ticket ticket)
    {
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Ticket {TicketGuid} updated in repository", ticket.Guid);
    }

    public async Task DeleteAsync(Guid id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket != null)
        {
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Ticket {TicketGuid} deleted from repository", id);
        }
    }

    public async Task<IEnumerable<Ticket>> GetActiveTicketsAsync()
    {
        return await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .Where(t => t.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetCompletedTicketsAsync()
    {
        return await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .Where(t => t.TicketStatus == Status.Completed)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Tickets.CountAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Tickets.AnyAsync(t => t.Guid == id);
    }
}
