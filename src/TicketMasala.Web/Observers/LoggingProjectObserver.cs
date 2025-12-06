using TicketMasala.Web.Models;

namespace TicketMasala.Web.Observers;

/// <summary>
/// Logging observer for project events.
/// Logs project lifecycle events for audit and debugging.
/// </summary>
public class LoggingProjectObserver : IProjectObserver
{
    private readonly ILogger<LoggingProjectObserver> _logger;

    public LoggingProjectObserver(ILogger<LoggingProjectObserver> logger)
    {
        _logger = logger;
    }

    public Task OnProjectCreatedAsync(Project project)
    {
        _logger.LogInformation(
            "Project created: {ProjectId} - {ProjectName} by Creator {CreatorId}",
            project.Guid,
            project.Name,
            project.CreatorGuid);
        return Task.CompletedTask;
    }

    public Task OnProjectUpdatedAsync(Project project)
    {
        _logger.LogInformation(
            "Project updated: {ProjectId} - {ProjectName}",
            project.Guid,
            project.Name);
        return Task.CompletedTask;
    }

    public Task OnProjectStatusChangedAsync(Project project, Status oldStatus, Status newStatus)
    {
        _logger.LogInformation(
            "Project status changed: {ProjectId} - {ProjectName} from {OldStatus} to {NewStatus}",
            project.Guid,
            project.Name,
            oldStatus,
            newStatus);
        return Task.CompletedTask;
    }

    public Task OnStakeholderAddedAsync(Project project, Customer stakeholder)
    {
        _logger.LogInformation(
            "Stakeholder added to project: {ProjectId} - Stakeholder {StakeholderId} ({StakeholderName})",
            project.Guid,
            stakeholder.Id,
            $"{stakeholder.FirstName} {stakeholder.LastName}");
        return Task.CompletedTask;
    }

}
