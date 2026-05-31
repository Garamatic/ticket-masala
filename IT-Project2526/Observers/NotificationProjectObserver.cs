using IT_Project2526.Models;
using IT_Project2526.Services;

namespace IT_Project2526.Observers;

/// <summary>
/// Notification observer for project events.
/// Sends notifications to stakeholders on project lifecycle events.
/// </summary>
public class NotificationProjectObserver : IProjectObserver
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationProjectObserver> _logger;

    public NotificationProjectObserver(
        INotificationService notificationService,
        ILogger<NotificationProjectObserver> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task OnProjectCreatedAsync(Project project)
    {
        try
        {
            // Notify project manager if assigned
            if (project.ProjectManager != null)
            {
                await _notificationService.NotifyUserAsync(
                    project.ProjectManager.Id,
                    $"You have been assigned as project manager for: {project.Name}",
                    $"/Projects/Details/{project.Guid}",
                    "Info");
            }

            // Notify primary customer
            if (project.Customer != null)
            {
                await _notificationService.NotifyUserAsync(
                    project.Customer.Id,
                    $"A new project has been created for you: {project.Name}",
                    $"/Projects/Details/{project.Guid}",
                    "Info");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send project creation notifications for {ProjectId}", project.Guid);
        }
    }

    public async Task OnProjectUpdatedAsync(Project project)
    {
        try
        {
            // Notify stakeholders about project updates
            foreach (var stakeholder in project.Customers)
            {
                await _notificationService.NotifyUserAsync(
                    stakeholder.Id,
                    $"Project '{project.Name}' has been updated.",
                    $"/Projects/Details/{project.Guid}",
                    "Info");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send project update notifications for {ProjectId}", project.Guid);
        }
    }

    public async Task OnProjectStatusChangedAsync(Project project, Status oldStatus, Status newStatus)
    {
        try
        {
            var statusMessage = $"Project '{project.Name}' status changed from {oldStatus} to {newStatus}";

            // Notify all stakeholders
            foreach (var stakeholder in project.Customers)
            {
                await _notificationService.NotifyUserAsync(
                    stakeholder.Id,
                    statusMessage,
                    $"/Projects/Details/{project.Guid}",
                    "Info");
            }

            // Notify project manager
            if (project.ProjectManager != null)
            {
                await _notificationService.NotifyUserAsync(
                    project.ProjectManager.Id,
                    statusMessage,
                    $"/Projects/Details/{project.Guid}",
                    "Info");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send project status change notifications for {ProjectId}", project.Guid);
        }
    }

    public async Task OnStakeholderAddedAsync(Project project, Customer stakeholder)
    {
        try
        {
            await _notificationService.NotifyUserAsync(
                stakeholder.Id,
                $"You have been added as a stakeholder to project: {project.Name}",
                $"/Projects/Details/{project.Guid}",
                "Info");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send stakeholder notification for {ProjectId}", project.Guid);
        }
    }
}
