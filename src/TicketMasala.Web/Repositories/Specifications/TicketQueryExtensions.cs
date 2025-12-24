using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Repositories.Specifications;

/// <summary>
/// Extension method-based specifications for composable Ticket queries.
/// Eliminates code duplication in repository filtering logic.
/// Complements the existing class-based Specification pattern with simpler query composition.
/// </summary>
public static class TicketQueryExtensions
{
    /// <summary>
    /// Filters tickets by department through Project relationship.
    /// This logic was duplicated 5+ times across different repository methods.
    /// </summary>
    public static IQueryable<Ticket> FilterByDepartment(
        this IQueryable<Ticket> query,
        Guid? departmentId,
        DbSet<Project> projects)
    {
        if (!departmentId.HasValue)
        {
            return query;
        }

        return query.Join(projects,
            ticket => ticket.ProjectGuid,
            project => project.Guid,
            (ticket, project) => new { Ticket = ticket, Project = project })
            .Where(x => x.Project.DepartmentId == departmentId.Value)
            .Select(x => x.Ticket);
    }

    /// <summary>
    /// Filters out soft-deleted tickets (ValidUntil != null).
    /// </summary>
    public static IQueryable<Ticket> FilterValid(this IQueryable<Ticket> query)
    {
        return query.Where(t => t.ValidUntil == null);
    }

    /// <summary>
    /// Filters tickets by status.
    /// </summary>
    public static IQueryable<Ticket> FilterByStatus(this IQueryable<Ticket> query, Status status)
    {
        return query.Where(t => t.TicketStatus == status);
    }

    /// <summary>
    /// Filters tickets by status (Pending or Assigned).
    /// </summary>
    public static IQueryable<Ticket> FilterPendingOrAssigned(this IQueryable<Ticket> query)
    {
        return query.Where(t => t.TicketStatus == Status.Pending || t.TicketStatus == Status.Assigned);
    }

    /// <summary>
    /// Filters unassigned tickets (Pending or Assigned with no ResponsibleId).
    /// </summary>
    public static IQueryable<Ticket> FilterUnassigned(this IQueryable<Ticket> query)
    {
        return query.Where(t => t.TicketStatus == Status.Pending ||
                               (t.TicketStatus == Status.Assigned && t.ResponsibleId == null));
    }

    /// <summary>
    /// Filters tickets created within a time window.
    /// </summary>
    public static IQueryable<Ticket> FilterRecent(this IQueryable<Ticket> query, int timeWindowMinutes)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);
        return query.Where(t => t.CreationDate >= cutoffTime);
    }
}
