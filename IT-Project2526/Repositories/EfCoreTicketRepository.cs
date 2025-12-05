using IT_Project2526.Models;
using IT_Project2526.ViewModels;
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

    public async Task<IEnumerable<Ticket>> GetAllAsync(Guid? departmentId = null)
    {
        var query = _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(t => t.Project != null && t.Project.DepartmentId == departmentId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetUnassignedAsync(Guid? departmentId = null)
    {
        var query = _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Project)
            .Where(t => t.ValidUntil == null)
            .Where(t => t.TicketStatus == Status.Pending || 
                       (t.TicketStatus == Status.Assigned && t.ResponsibleId == null));

        if (departmentId.HasValue)
        {
            query = query.Where(t => t.Project != null && t.Project.DepartmentId == departmentId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByStatusAsync(Status status, Guid? departmentId = null)
    {
        var query = _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .Where(t => t.TicketStatus == status && t.ValidUntil == null);

        if (departmentId.HasValue)
        {
            query = query.Where(t => t.Project != null && t.Project.DepartmentId == departmentId.Value);
        }

        return await query.ToListAsync();
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

    public async Task<IEnumerable<Ticket>> GetRecentAsync(int timeWindowMinutes, Guid? departmentId = null)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);
        var query = _context.Tickets
            .Where(t => t.CreationDate >= cutoffTime)
            .Where(t => t.ValidUntil == null);

        if (departmentId.HasValue)
        {
            query = query.Where(t => t.Project != null && t.Project.DepartmentId == departmentId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetPendingOrAssignedAsync(Guid? departmentId = null)
    {
        var query = _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .Where(t => t.TicketStatus == Status.Pending || t.TicketStatus == Status.Assigned)
            .Where(t => t.ValidUntil == null);

        if (departmentId.HasValue)
        {
            query = query.Where(t => t.Project != null && t.Project.DepartmentId == departmentId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel, Guid? departmentId = null)
    {
        var query = _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .Include(t => t.Project)
            .Where(t => t.ValidUntil == null)
            .AsQueryable();

        // Apply Department Filter
        if (departmentId.HasValue)
        {
            query = query.Where(t => t.Project != null && t.Project.DepartmentId == departmentId.Value);
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchModel.SearchTerm))
        {
            var term = searchModel.SearchTerm.ToLower();
            query = query.Where(t => 
                t.Description.ToLower().Contains(term) || 
                (t.Customer != null && (t.Customer.FirstName.ToLower().Contains(term) || t.Customer.LastName.ToLower().Contains(term))) ||
                (t.Project != null && t.Project.Name.ToLower().Contains(term))
            );
        }

        if (searchModel.Status.HasValue)
        {
            query = query.Where(t => t.TicketStatus == searchModel.Status.Value);
        }

        if (searchModel.TicketType.HasValue)
        {
            query = query.Where(t => t.TicketType == searchModel.TicketType.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchModel.ResponsibleId))
        {
            query = query.Where(t => t.ResponsibleId == searchModel.ResponsibleId);
        }

        if (!string.IsNullOrWhiteSpace(searchModel.CustomerId))
        {
            query = query.Where(t => t.CustomerId == searchModel.CustomerId);
        }

        if (searchModel.DateFrom.HasValue)
        {
            query = query.Where(t => t.CreationDate >= searchModel.DateFrom.Value);
        }

        if (searchModel.DateTo.HasValue)
        {
            query = query.Where(t => t.CreationDate <= searchModel.DateTo.Value);
        }
        
        // Count total items before pagination
        searchModel.TotalItems = await query.CountAsync();

        // Apply pagination
        searchModel.Results = await query
            .OrderByDescending(t => t.CreationDate)
            .Skip((searchModel.Page - 1) * searchModel.PageSize)
            .Take(searchModel.PageSize)
            .ToListAsync();

        return searchModel;
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
