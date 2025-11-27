using IT_Project2526.ViewModels;

namespace IT_Project2526.Services
{
    /// <summary>
    /// Service interface for project business logic
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// Gets all projects as view models
        /// </summary>
        Task<IEnumerable<ProjectTicketViewModel>> GetAllProjectsAsync();
        
        /// <summary>
        /// Gets a project by ID with all details
        /// </summary>
        Task<ProjectTicketViewModel?> GetProjectByIdAsync(Guid id);
        
        /// <summary>
        /// Creates a new project from the view model
        /// </summary>
        Task<Guid> CreateProjectAsync(NewProject model, string currentUserId);
        
        /// <summary>
        /// Updates an existing project
        /// </summary>
        Task UpdateProjectAsync(Guid id, NewProject model);
        
        /// <summary>
        /// Deletes (soft delete) a project
        /// </summary>
        Task DeleteProjectAsync(Guid id);
        
        /// <summary>
        /// Gets all projects for a specific customer
        /// </summary>
        Task<IEnumerable<ProjectTicketViewModel>> GetCustomerProjectsAsync(string customerId);
        
        /// <summary>
        /// Gets all projects managed by an employee
        /// </summary>
        Task<IEnumerable<ProjectTicketViewModel>> GetManagerProjectsAsync(string managerId);
        
        /// <summary>
        /// Searches projects by name
        /// </summary>
        Task<IEnumerable<ProjectTicketViewModel>> SearchProjectsAsync(string searchTerm);
        
        /// <summary>
        /// Assigns a project manager to a project
        /// </summary>
        Task AssignProjectManagerAsync(Guid projectId, string managerId);
        
        /// <summary>
        /// Updates project status
        /// </summary>
        Task UpdateProjectStatusAsync(Guid projectId, Models.Status newStatus);
        
        /// <summary>
        /// Gets project statistics for a customer
        /// </summary>
        Task<Repositories.ProjectStatistics> GetCustomerStatisticsAsync(string customerId);
    }
}
