using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Abstractions;

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
    /// Simplified to use navigation properties (KISS).
    /// </summary>
    public static IQueryable<Ticket> FilterByDepartment(
        this IQueryable<Ticket> query,
        Guid? departmentId)
    {
        if (!departmentId.HasValue)
        {
            return query;
        }

        return query.Where(t => t.Project != null && t.Project.DepartmentId == departmentId.Value);
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
    public static IQueryable<Ticket> WithinTimeWindow(this IQueryable<Ticket> query, int timeWindowMinutes, ISystemClock clock)
    {
        var cutoffTime = clock.UtcNow.AddMinutes(-timeWindowMinutes);
        return query.Where(t => t.CreationDate >= cutoffTime);
    }

    /// <summary>
    /// Filters tickets by search term (Title, Description, Customer Name, Project Name).
    /// </summary>
    public static IQueryable<Ticket> FilterBySearchTerm(this IQueryable<Ticket> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var term = searchTerm.ToLower();
        return query.Where(t =>
            (t.Title != null && t.Title.ToLower().Contains(term)) ||
            t.Description.ToLower().Contains(term) ||
            (t.Customer != null && (t.Customer.FirstName.ToLower().Contains(term) || t.Customer.LastName.ToLower().Contains(term))) ||
            (t.Project != null && t.Project.Name.ToLower().Contains(term)));
    }
}
