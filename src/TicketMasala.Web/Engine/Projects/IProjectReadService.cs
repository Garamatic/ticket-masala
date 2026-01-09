using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.ViewModels.Projects;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TicketMasala.Web.Engine.Projects;

public interface IProjectReadService
{
    Task<IEnumerable<ProjectTicketViewModel>> GetAllProjectsAsync(string? userId, bool isCustomer);
    Task<ProjectTicketViewModel?> GetProjectDetailsAsync(Guid projectGuid);
    Task<NewProject?> GetProjectForEditAsync(Guid projectGuid);
    Task<IEnumerable<SelectListItem>> GetCustomerSelectListAsync(string? selectedCustomerId = null);
    Task<IEnumerable<SelectListItem>> GetStakeholderSelectListAsync();
    Task<IEnumerable<SelectListItem>> GetTemplateSelectListAsync();
    Task<SelectList> GetEmployeeSelectListAsync(string? selectedId = null);
    Task<IEnumerable<ProjectTicketViewModel>> GetProjectsByCustomerAsync(string customerId);
    Task<IEnumerable<ProjectTicketViewModel>> SearchProjectsAsync(string query);
    Task<ProjectStatisticsViewModel> GetProjectStatisticsAsync(string customerId);
    Task<Guid?> GetProjectIdForTicketAsync(Guid ticketId);
    Task<CreateProjectFromTicketViewModel?> PrepareCreateFromTicketViewModelAsync(Guid ticketId);
}
