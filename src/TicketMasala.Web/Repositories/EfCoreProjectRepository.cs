using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Repositories;

/// <summary>
/// EF Core implementation of IProjectRepository.
/// </summary>
public class EfCoreProjectRepository : IProjectRepository
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<EfCoreProjectRepository> _logger;

    public EfCoreProjectRepository(MasalaDbContext context, ILogger<EfCoreProjectRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Project?> GetByIdAsync(Guid id, bool includeRelations = true)
    {
        // Note: Navigation properties removed from Domain models
        // Relationships configured in MasalaDbContext.ConfigureUserRelationships()
        var query = _context.Projects.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(p => p.Customer)
                .Include(p => p.ProjectManager);
        }

        return await query.FirstOrDefaultAsync(p => p.Guid == id);
    }

    public async Task<IEnumerable<Project>> GetAllAsync()
    {
        return await _context.Projects
            .Where(p => p.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Project>> GetActiveProjectsAsync()
    {
        return await _context.Projects
            .Where(p => p.Status == Status.Pending || p.Status == Status.InProgress)
            .Where(p => p.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Project>> GetByCustomerIdAsync(string customerId)
    {
        return await _context.Projects
            .Where(p => p.CustomerId == customerId)
            .Where(p => p.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<Project?> GetRecommendedProjectForCustomerAsync(string customerId)
    {
        return await _context.Projects
            .Where(p => p.CustomerId == customerId && p.ValidUntil == null)
            .Where(p => p.Status == Status.Pending || p.Status == Status.InProgress)
            .OrderByDescending(p => p.CreationDate)
            .FirstOrDefaultAsync();
    }

    public async Task<Project> AddAsync(Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Project {ProjectGuid} added to repository", project.Guid);
        return project;
    }

    public async Task UpdateAsync(Project project)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Project {ProjectGuid} updated in repository", project.Guid);
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Project {ProjectGuid} deleted from repository", id);
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Projects.AnyAsync(p => p.Guid == id);
    }

}
