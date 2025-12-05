using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IT_Project2526.Services;

/// <summary>
/// Service interface for project business logic.
/// Follows the same pattern as ITicketService for consistency.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Get all projects accessible to the user
    /// </summary>
    Task<IEnumerable<ProjectTicketViewModel>> GetAllProjectsAsync(string? userId, bool isCustomer);

    /// <summary>
    /// Get project details with tasks
    /// </summary>
    Task<ProjectTicketViewModel?> GetProjectDetailsAsync(Guid projectGuid);

    /// <summary>
    /// Get project for editing
    /// </summary>
    Task<NewProject?> GetProjectForEditAsync(Guid projectGuid);

    /// <summary>
    /// Create a new project with optional template
    /// </summary>
    Task<Project> CreateProjectAsync(NewProject viewModel, string userId);

    /// <summary>
    /// Update an existing project
    /// </summary>
    Task<bool> UpdateProjectAsync(Guid projectGuid, NewProject viewModel);

    /// <summary>
    /// Get customer dropdown list
    /// </summary>
    Task<List<SelectListItem>> GetCustomerSelectListAsync(string? selectedCustomerId = null);

    /// <summary>
    /// Get stakeholder dropdown list
    /// </summary>
    Task<List<SelectListItem>> GetStakeholderSelectListAsync();

    /// <summary>
    /// Get project templates dropdown list
    /// </summary>
    Task<List<SelectListItem>> GetTemplateSelectListAsync();
}
