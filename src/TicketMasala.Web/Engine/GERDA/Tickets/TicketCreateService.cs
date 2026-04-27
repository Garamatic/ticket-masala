using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Facades;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service for ticket create view operations.
/// Single responsibility: Create view concerns only.
/// </summary>
public interface ITicketCreateService
{
    Task<TicketCreateContext> GetCreateContextAsync(bool isCustomer, string? preselectedCustomerId = null, Guid? projectGuid = null);
}

public class TicketCreateService : ITicketCreateService
{
    private readonly ITicketReadService _ticketReadService;
    private readonly IProjectReadService _projectReadService;

    public TicketCreateService(
        ITicketReadService ticketReadService,
        IProjectReadService projectReadService)
    {
        _ticketReadService = ticketReadService;
        _projectReadService = projectReadService;
    }

    public async Task<TicketCreateContext> GetCreateContextAsync(bool isCustomer, string? preselectedCustomerId = null, Guid? projectGuid = null)
    {
        var context = new TicketCreateContext
        {
            IsCustomer = isCustomer,
            Employees = await _ticketReadService.GetEmployeeSelectListAsync(),
            Projects = await _ticketReadService.GetProjectSelectListAsync()
        };

        if (projectGuid.HasValue)
        {
            var project = await _projectReadService.GetProjectDetailsAsync(projectGuid.Value);
            if (project != null && project.ProjectDetails != null)
            {
                context.PreselectedProjectId = project.ProjectDetails.Guid;
                if (!string.IsNullOrEmpty(project.ProjectDetails.CustomerId))
                {
                    preselectedCustomerId = project.ProjectDetails.CustomerId;
                }
            }
        }

        if (!isCustomer)
        {
            context.Customers = await _ticketReadService.GetCustomerSelectListAsync();
            context.PreselectedCustomerId = preselectedCustomerId;
        }
        else
        {
            context.PreselectedCustomerId = preselectedCustomerId;
        }

        // Note: Domain configuration is set by the caller using IDomainConfigurationService
        // This keeps the service focused on data fetching only

        return context;
    }
}
