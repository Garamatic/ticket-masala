using TicketMasala.Web.Models;
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
        /// </summary>
        public static IQueryable<Project> IncludeDetails(this IQueryable<Project> query)
        {
            return query
                .Include(p => p.Tasks)
                .Include(p => p.ProjectManager)
                .Include(p => p.Customer)
                .Include(p => p.Resources);
        }
        
        /// <summary>
        /// Includes related data for Tickets
        /// </summary>
        public static IQueryable<Ticket> IncludeDetails(this IQueryable<Ticket> query)
        {
            return query
                .Include(t => t.Responsible)
                .Include(t => t.Customer)
                .Include(t => t.Watchers)
                .Include(t => t.ParentTicket)
                .Include(t => t.SubTickets);
        }
        
        /// <summary>
        /// Filters projects by status
        /// </summary>
        public static IQueryable<Project> WithStatus(this IQueryable<Project> query, Status status)
        {
            return query.Where(p => p.Status == status);
        }
        
        /// <summary>
        /// Filters tickets by status
        /// </summary>
        public static IQueryable<Ticket> WithStatus(this IQueryable<Ticket> query, Status status)
        {
            return query.Where(t => t.TicketStatus == status);
        }
        
        /// <summary>
        /// Filters projects by customer
        /// </summary>
        public static IQueryable<Project> ForCustomer(this IQueryable<Project> query, string customerId)
        {
            return query.Where(p => p.CustomerId == customerId);
        }
        
        /// <summary>
        /// Filters tickets by customer
        /// </summary>
        public static IQueryable<Ticket> ForCustomer(this IQueryable<Ticket> query, string customerId)
        {
            return query.Where(t => t.Customer.Id == customerId);
        }
        
        /// <summary>
        /// Returns overdue tickets
        /// </summary>
        public static IQueryable<Ticket> WhereOverdue(this IQueryable<Ticket> query)
        {
            var now = DateTime.UtcNow;
            return query.Where(t => t.CompletionTarget.HasValue 
                                 && t.CompletionTarget.Value < now
                                 && t.TicketStatus != Status.Completed
                                 && t.TicketStatus != Status.Failed);
        }
}
