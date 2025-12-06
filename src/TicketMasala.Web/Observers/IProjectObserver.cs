using TicketMasala.Web.Models;

namespace TicketMasala.Web.Observers;

/// <summary>
/// Observer interface for project lifecycle events.
/// Implements Observer pattern for event-driven architecture.
/// Mirrors ITicketObserver for architectural consistency.
/// </summary>
public interface IProjectObserver
{
    /// <summary>
    /// Called when a new project is created
    /// </summary>
    Task OnProjectCreatedAsync(Project project);

    /// <summary>
    /// Called when a project is updated
    /// </summary>
    Task OnProjectUpdatedAsync(Project project);

    /// <summary>
    /// Called when a project status changes
    /// </summary>
    Task OnProjectStatusChangedAsync(Project project, Status oldStatus, Status newStatus);

    /// <summary>
    /// Called when a stakeholder is added to a project
    /// </summary>
    Task OnStakeholderAddedAsync(Project project, Customer stakeholder);

}
