using IT_Project2526.Models;

namespace IT_Project2526.Repositories
{
    /// <summary>
    /// Repository interface for Project-specific operations
    /// </summary>
    public interface IProjectRepository : IRepository<Project>
    {
        /// <summary>
        /// Gets all projects with their related data (tasks, manager, customer)
        /// </summary>
        Task<IEnumerable<Project>> GetAllWithDetailsAsync();
        
        /// <summary>
        /// Gets a project by ID with all related data
        /// </summary>
        Task<Project?> GetByIdWithDetailsAsync(Guid id);
        
        /// <summary>
        /// Gets all projects for a specific customer
        /// </summary>
        Task<IEnumerable<Project>> GetByCustomerIdAsync(string customerId);
        
        /// <summary>
        /// Gets all projects managed by a specific employee
        /// </summary>
        Task<IEnumerable<Project>> GetByProjectManagerIdAsync(string managerId);
        
        /// <summary>
        /// Gets projects by status
        /// </summary>
        Task<IEnumerable<Project>> GetByStatusAsync(Status status);
        
        /// <summary>
        /// Gets projects created within a date range
        /// </summary>
        Task<IEnumerable<Project>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Searches projects by name (partial match)
        /// </summary>
        Task<IEnumerable<Project>> SearchByNameAsync(string searchTerm);
        
        /// <summary>
        /// Gets project statistics for a customer
        /// </summary>
        Task<ProjectStatistics> GetCustomerStatisticsAsync(string customerId);
    }
    
    /// <summary>
    /// Project statistics data
    /// </summary>
    public class ProjectStatistics
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int PendingProjects { get; set; }
        public int InProgressProjects { get; set; }
    }
}
