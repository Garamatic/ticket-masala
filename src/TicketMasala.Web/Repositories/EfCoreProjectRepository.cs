using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;

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

    public async Task<IReadOnlyList<Project>> GetAllAsync()
    {
        return await _context.Projects
            .Where(p => p.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Project>> GetActiveProjectsAsync()
    {
        return await _context.Projects
            .Where(p => p.Status == Status.Pending || p.Status == Status.InProgress)
            .Where(p => p.ValidUntil == null)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Project>> GetByCustomerIdAsync(string customerId)
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

    public Task<Project> AddAsync(Project project)
    {
        _context.Projects.Add(project);
        // Note: Changes are not committed here. Call IUnitOfWork.CommitAsync() to persist.
        _logger.LogDebug("Project {ProjectGuid} queued for add (pending commit)", project.Guid);
        return Task.FromResult(project);
    }

    public Task UpdateAsync(Project project)
    {
        _context.Projects.Update(project);
        // Note: Changes are not committed here. Call IUnitOfWork.CommitAsync() to persist.
        _logger.LogDebug("Project {ProjectGuid} queued for update (pending commit)", project.Guid);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            _context.Projects.Remove(project);
            // Note: Changes are not committed here. Call IUnitOfWork.CommitAsync() to persist.
            _logger.LogDebug("Project {ProjectGuid} queued for delete (pending commit)", id);
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Projects.AnyAsync(p => p.Guid == id);
    }

}
