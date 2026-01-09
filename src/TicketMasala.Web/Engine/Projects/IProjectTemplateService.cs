using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.Projects;

public interface IProjectTemplateService
{
    Task ApplyTemplateAsync(Project project, Guid templateId);
}
