using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.ViewModels.Projects;

namespace TicketMasala.Web.Engine.Projects;

public interface IProjectWorkflowService
{
    Task<Project> CreateProjectAsync(NewProject viewModel, string userId);
    Task<bool> UpdateProjectAsync(Guid projectGuid, NewProject viewModel);
    Task<bool> UpdateProjectStatusAsync(Guid projectGuid, Status status);
    Task<bool> AssignProjectManagerAsync(Guid projectGuid, string managerId);
    Task<bool> DeleteProjectAsync(Guid projectGuid);
    Task<Guid?> CreateProjectFromTicketAsync(CreateProjectFromTicketViewModel viewModel, string userId);
}
