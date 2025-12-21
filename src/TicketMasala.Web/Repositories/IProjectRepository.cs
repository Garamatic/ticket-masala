using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Repositories;

/// <summary>
/// Repository interface for Project entity operations.
/// </summary>
public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<IEnumerable<Project>> GetAllAsync();
    Task<IEnumerable<Project>> GetActiveProjectsAsync();
    Task<IEnumerable<Project>> GetByCustomerIdAsync(string customerId);
    Task<Project?> GetRecommendedProjectForCustomerAsync(string customerId);
    Task<Project> AddAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);

}
