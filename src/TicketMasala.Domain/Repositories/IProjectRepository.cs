using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Repositories;

/// <summary>
/// Repository interface for Project entity operations.
/// </summary>
public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<IReadOnlyList<Project>> GetAllAsync();
    Task<IReadOnlyList<Project>> GetActiveProjectsAsync();
    Task<IReadOnlyList<Project>> GetByCustomerIdAsync(string customerId);
    Task<Project?> GetRecommendedProjectForCustomerAsync(string customerId);
    Task<Project> AddAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
