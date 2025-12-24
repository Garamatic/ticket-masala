using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Data;
using TicketMasala.Web.Repositories.Queries;
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

    public async Task<(IEnumerable<TicketSearchResultDto> Results, int TotalItems)> SearchAsync(TicketSearchQuery query)
    {
        var dbQuery = _context.Tickets
            .FilterValid()
            .FilterByDepartment(query.DepartmentId, _context.Projects);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            // Join with Users and Projects for search
            dbQuery = dbQuery
                .Include(t => t.Customer)
                .Include(t => t.Responsible)
                .Include(t => t.Project)
                .Where(t =>
                    t.Description.ToLower().Contains(term) ||
                    (t.Customer != null && (t.Customer.FirstName.ToLower().Contains(term) || t.Customer.LastName.ToLower().Contains(term))) ||
                    (t.Project != null && t.Project.Name.ToLower().Contains(term)));
        }

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.TicketStatus == query.Status.Value);
        }

        if (query.TicketType.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.TicketType == query.TicketType.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ResponsibleId))
        {
            dbQuery = dbQuery.Where(t => t.ResponsibleId == query.ResponsibleId);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerId))
        {
            dbQuery = dbQuery.Where(t => t.CustomerId == query.CustomerId);
        }

        if (query.DateFrom.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.CreationDate >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.CreationDate <= query.DateTo.Value);
        }

        if (query.ProjectId.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.ProjectGuid == query.ProjectId.Value);
        }

        // Count total items before pagination
        var totalItems = await dbQuery.CountAsync();

        // Apply pagination and projection to DTO
        var results = await dbQuery
            .OrderByDescending(t => t.CreationDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TicketSearchResultDto
            {
                Guid = t.Guid,
                Title = t.Title ?? string.Empty,
                Description = t.Description,
                TicketStatus = t.TicketStatus,
                CreationDate = t.CreationDate,
                CompletionTarget = t.CompletionTarget,
                CustomerName = t.Customer != null ? t.Customer.FirstName + " " + t.Customer.LastName : "Unknown",
                ResponsibleName = t.Responsible != null ? t.Responsible.FirstName + " " + t.Responsible.LastName : "Not Assigned",
                ProjectName = t.Project != null ? t.Project.Name : null,
                ProjectGuid = t.ProjectGuid,
                GerdaTags = t.GerdaTags
            })
            .ToListAsync();

        return (results, totalItems);
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
