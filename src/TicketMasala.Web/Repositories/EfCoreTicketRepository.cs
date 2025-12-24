using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.Data;
using TicketMasala.Web.Repositories.Specifications;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Repositories;

/// <summary>
/// EF Core implementation of ITicketRepository.
/// Adapter pattern - adapts EF Core DbContext to domain repository interface.
/// </summary>
public class EfCoreTicketRepository : ITicketRepository
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<EfCoreTicketRepository> _logger;

    public EfCoreTicketRepository(MasalaDbContext context, ILogger<EfCoreTicketRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations = true)
    {
        // Note: Navigation properties removed from Domain models
        // Relationships configured in MasalaDbContext.ConfigureUserRelationships()
        var query = _context.Tickets.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(t => t.Customer)
                .Include(t => t.Responsible)
                .Include(t => t.Project);
        }

        return await query.FirstOrDefaultAsync(t => t.Guid == id);
    }

    public async Task<IEnumerable<Ticket>> GetAllAsync(Guid? departmentId = null)
    {
        return await _context.Tickets
            .FilterByDepartment(departmentId, _context.Projects)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetUnassignedAsync(Guid? departmentId = null)
    {
        return await _context.Tickets
            .FilterValid()
            .FilterUnassigned()
            .FilterByDepartment(departmentId, _context.Projects)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByStatusAsync(Status status, Guid? departmentId = null)
    {
        return await _context.Tickets
            .FilterByStatus(status)
            .FilterValid()
            .FilterByDepartment(departmentId, _context.Projects)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByCustomerIdAsync(string customerId)
    {
        return await _context.Tickets
            .Where(t => t.CustomerId == customerId)
            .FilterValid()
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByResponsibleIdAsync(string responsibleId)
    {
        return await _context.Tickets
            .Where(t => t.ResponsibleId == responsibleId)
            .FilterValid()
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByProjectGuidAsync(Guid projectGuid)
    {
        return await _context.Tickets
            .Where(t => t.ProjectGuid == projectGuid)
            .FilterValid()
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetRecentAsync(int timeWindowMinutes, Guid? departmentId = null)
    {
        return await _context.Tickets
            .FilterRecent(timeWindowMinutes)
            .FilterValid()
            .FilterByDepartment(departmentId, _context.Projects)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetPendingOrAssignedAsync(Guid? departmentId = null)
    {
        return await _context.Tickets
            .FilterPendingOrAssigned()
            .FilterValid()
            .FilterByDepartment(departmentId, _context.Projects)
            .ToListAsync();
    }

    public async Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel, Guid? departmentId = null)
    {
        var query = _context.Tickets
            .FilterValid()
            .FilterByDepartment(departmentId, _context.Projects);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchModel.SearchTerm))
        {
            var term = searchModel.SearchTerm.ToLower();
            // Join with Users and Projects for search
            query = query
                .Include(t => t.Customer)
                .Include(t => t.Responsible)
                .Include(t => t.Project)
                .Where(t =>
                    t.Description.ToLower().Contains(term) ||
                    (t.Customer != null && (t.Customer.FirstName.ToLower().Contains(term) || t.Customer.LastName.ToLower().Contains(term))) ||
                    (t.Project != null && t.Project.Name.ToLower().Contains(term)));
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
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .Where(t => t.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetCompletedTicketsAsync()
    {
        return await _context.Tickets
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

    public async Task<IEnumerable<Document>> GetDocumentsForTicketAsync(Guid ticketId)
    {
        return await _context.Documents
            .Where(d => d.TicketId == ticketId)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TicketComment>> GetCommentsForTicketAsync(Guid ticketId)
    {
        return await _context.TicketComments
            .Include(c => c.Author)
            .Where(c => c.TicketId == ticketId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<QualityReview>> GetQualityReviewsForTicketAsync(Guid ticketId)
    {
        return await _context.QualityReviews
            .Where(r => r.TicketId == ticketId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

}
