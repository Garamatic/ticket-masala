using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.ViewModels.Customers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TicketMasala.Web.Engine.Projects;

/// <summary>
/// Service interface for project business logic.
/// Follows the same pattern as ITicketService for consistency.
/// </summary>
[Obsolete("Use IProjectReadService, IProjectWorkflowService, or IProjectTemplateService instead.")]
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
    Task<IEnumerable<SelectListItem>> GetCustomerSelectListAsync(string? selectedCustomerId = null);

    /// <summary>
    /// Get stakeholder dropdown list
    /// </summary>
    Task<IEnumerable<SelectListItem>> GetStakeholderSelectListAsync();

    /// <summary>
    /// Get project templates dropdown list
    /// </summary>
    Task<IEnumerable<SelectListItem>> GetTemplateSelectListAsync();

    /// <summary>
    /// Prepare ViewModel for creating a project from a ticket
    /// Includes GERDA PM recommendation
    /// </summary>
    Task<CreateProjectFromTicketViewModel?> PrepareCreateFromTicketViewModelAsync(Guid ticketId);

    /// <summary>
    /// Create a project from an existing ticket with template and PM assignment
    /// </summary>
    Task<Guid?> CreateProjectFromTicketAsync(CreateProjectFromTicketViewModel viewModel, string userId);

    /// <summary>
    /// Get employee dropdown list for PM selection
    /// </summary>
    Task<SelectList> GetEmployeeSelectListAsync(string? selectedId = null);

    /// <summary>
    /// Get all projects for a specific customer
    /// </summary>
    Task<IEnumerable<ProjectTicketViewModel>> GetProjectsByCustomerAsync(string customerId);

    /// <summary>
    /// Search projects by name or description
    /// </summary>
    Task<IEnumerable<ProjectTicketViewModel>> SearchProjectsAsync(string query);

    /// <summary>
    /// Get project statistics for a customer
    /// </summary>
    Task<ProjectStatisticsViewModel> GetProjectStatisticsAsync(string customerId);

    /// <summary>
    /// Update project status
    /// </summary>
    Task<bool> UpdateProjectStatusAsync(Guid projectGuid, Status status);

    /// <summary>
    /// Assign project manager
    /// </summary>
    Task<bool> AssignProjectManagerAsync(Guid projectGuid, string managerId);

    /// <summary>
    /// Soft delete project
    /// </summary>
    Task<bool> DeleteProjectAsync(Guid projectGuid);

    /// <summary>
    /// Get project ID for a ticket (if ticket belongs to a project)
    /// </summary>
    Task<Guid?> GetProjectIdForTicketAsync(Guid ticketId);

}
