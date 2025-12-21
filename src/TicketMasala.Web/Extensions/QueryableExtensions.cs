using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods for IQueryable to simplify common query patterns
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Returns only active (not soft-deleted) entities
    /// </summary>
    public static IQueryable<T> WhereActive<T>(this IQueryable<T> query)
        where T : BaseModel
    {
        return query.Where(e => e.ValidUntil == null);
    }

    /// <summary>
    /// Returns only inactive (soft-deleted) entities
    /// </summary>
    public static IQueryable<T> WhereInactive<T>(this IQueryable<T> query)
        where T : BaseModel
    {
        return query.Where(e => e.ValidUntil != null);
    }

    /// <summary>
    /// Applies pagination to a query
    /// </summary>
    /// <param name="query">The query to paginate</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        return query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// Orders entities by creation date descending (newest first)
    /// </summary>
    public static IQueryable<T> OrderByNewest<T>(this IQueryable<T> query)
        where T : BaseModel
    {
        return query.OrderByDescending(e => e.CreationDate);
    }

    /// <summary>
    /// Orders entities by creation date ascending (oldest first)
    /// </summary>
    public static IQueryable<T> OrderByOldest<T>(this IQueryable<T> query)
        where T : BaseModel
    {
        return query.OrderBy(e => e.CreationDate);
    }

    /// <summary>
    /// Includes related data for Projects
    /// Note: Navigation properties are configured via EF Core in MasalaDbContext
    /// </summary>
    public static IQueryable<Domain.Entities.Project> IncludeDetails(this IQueryable<Domain.Entities.Project> query)
    {
        // Navigation properties are configured in MasalaDbContext.ConfigureUserRelationships()
        // This method can be extended if needed for specific includes
        return query;
    }

    /// <summary>
    /// Includes related data for Tickets
    /// Note: Navigation properties are configured via EF Core in MasalaDbContext
    /// </summary>
    public static IQueryable<Domain.Entities.Ticket> IncludeDetails(this IQueryable<Domain.Entities.Ticket> query)
    {
        // Navigation properties are configured in MasalaDbContext.ConfigureUserRelationships()
        // This method can be extended if needed for specific includes
        return query;
    }

    /// <summary>
    /// Filters projects by status
    /// </summary>
    public static IQueryable<Domain.Entities.Project> WithStatus(this IQueryable<Domain.Entities.Project> query, Domain.Common.Status status)
    {
        return query.Where(p => p.Status == status);
    }

    /// <summary>
    /// Filters tickets by status
    /// </summary>
    public static IQueryable<Domain.Entities.Ticket> WithStatus(this IQueryable<Domain.Entities.Ticket> query, Domain.Common.Status status)
    {
        return query.Where(t => t.TicketStatus == status);
    }

    /// <summary>
    /// Filters projects by customer
    /// </summary>
    public static IQueryable<Domain.Entities.Project> ForCustomer(this IQueryable<Domain.Entities.Project> query, string customerId)
    {
        return query.Where(p => p.CustomerId == customerId);
    }

    /// <summary>
    /// Filters tickets by customer
    /// </summary>
    public static IQueryable<Domain.Entities.Ticket> ForCustomer(this IQueryable<Domain.Entities.Ticket> query, string customerId)
    {
        return query.Where(t => t.CustomerId == customerId);
    }

    /// <summary>
    /// Returns overdue tickets
    /// </summary>
    public static IQueryable<Domain.Entities.Ticket> WhereOverdue(this IQueryable<Domain.Entities.Ticket> query)
    {
        var now = DateTime.UtcNow;
        return query.Where(t => t.CompletionTarget.HasValue
                             && t.CompletionTarget.Value < now
                             && t.TicketStatus != Domain.Common.Status.Completed
                             && t.TicketStatus != Domain.Common.Status.Failed);
    }
}
