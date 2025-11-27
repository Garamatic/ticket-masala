using IT_Project2526.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Repositories
{
    /// <summary>
    /// Repository implementation for Project-specific operations
    /// </summary>
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(ITProjectDB context, ILogger<Repository<Project>> logger) 
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Project>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(p => p.Tasks)
                .Include(p => p.ProjectManager)
                .Include(p => p.Customer)
                .Include(p => p.Resources)
                .Where(p => p.ValidUntil == null)
                .OrderByDescending(p => p.CreationDate)
                .ToListAsync();
        }

        public async Task<Project?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Responsible)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Watchers)
                .Include(p => p.ProjectManager)
                .Include(p => p.Customer)
                .Include(p => p.Resources)
                .FirstOrDefaultAsync(p => p.Guid == id && p.ValidUntil == null);
        }

        public async Task<IEnumerable<Project>> GetByCustomerIdAsync(string customerId)
        {
            return await _dbSet
                .Include(p => p.ProjectManager)
                .Include(p => p.Tasks)
                .Where(p => p.CustomerId == customerId && p.ValidUntil == null)
                .OrderByDescending(p => p.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetByProjectManagerIdAsync(string managerId)
        {
            return await _dbSet
                .Include(p => p.Customer)
                .Include(p => p.Tasks)
                .Where(p => p.ProjectManagerId == managerId && p.ValidUntil == null)
                .OrderByDescending(p => p.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetByStatusAsync(Status status)
        {
            return await _dbSet
                .Include(p => p.ProjectManager)
                .Include(p => p.Customer)
                .Where(p => p.Status == status && p.ValidUntil == null)
                .OrderByDescending(p => p.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Include(p => p.ProjectManager)
                .Include(p => p.Customer)
                .Where(p => p.CreationDate >= startDate 
                         && p.CreationDate <= endDate 
                         && p.ValidUntil == null)
                .OrderByDescending(p => p.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> SearchByNameAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveAsync();

            return await _dbSet
                .Include(p => p.ProjectManager)
                .Include(p => p.Customer)
                .Where(p => p.Name.Contains(searchTerm) && p.ValidUntil == null)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<ProjectStatistics> GetCustomerStatisticsAsync(string customerId)
        {
            var projects = await _dbSet
                .Where(p => p.CustomerId == customerId && p.ValidUntil == null)
                .ToListAsync();

            return new ProjectStatistics
            {
                TotalProjects = projects.Count,
                ActiveProjects = projects.Count(p => p.Status == Status.InProgress),
                CompletedProjects = projects.Count(p => p.Status == Status.Completed),
                PendingProjects = projects.Count(p => p.Status == Status.Pending),
                InProgressProjects = projects.Count(p => p.Status == Status.InProgress)
            };
        }
    }
}
