using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Services;

/// <summary>
/// Domain service for managing ticket parent/child relationships (grouping).
/// Handles cross-aggregate concerns involving multiple tickets.
/// </summary>
public interface ITicketGroupingService
{
    /// <summary>
    /// Groups related tickets under a parent ticket.
    /// </summary>
    /// <param name="parentTicket">The parent ticket</param>
    /// <param name="childTickets">Tickets to group under the parent</param>
    /// <param name="groupedByUserId">User performing the grouping</param>
    /// <exception cref="DomainRuleException">Thrown when grouping violates rules</exception>
    Task GroupTicketsAsync(
        Ticket parentTicket,
        IEnumerable<Ticket> childTickets,
        string groupedByUserId);

    /// <summary>
    /// Removes a ticket from its parent group.
    /// </summary>
    Task UngroupTicketAsync(
        Ticket childTicket,
        string ungroupedByUserId);

    /// <summary>
    /// Splits a parent ticket into multiple child tickets.
    /// </summary>
    /// <param name="parentTicket">The ticket to split</param>
    /// <param name="splitDescriptions">Descriptions for each child ticket</param>
    /// <param name="splitByUserId">User performing the split</param>
    /// <returns>The created child tickets</returns>
    Task<IReadOnlyList<Ticket>> SplitTicketAsync(
        Ticket parentTicket,
        IEnumerable<string> splitDescriptions,
        string splitByUserId);

    /// <summary>
    /// Merges multiple tickets into a single ticket.
    /// The first ticket becomes the parent, others become children.
    /// </summary>
    Task<Ticket> MergeTicketsAsync(
        IEnumerable<Ticket> ticketsToMerge,
        string mergedByUserId);

    /// <summary>
    /// Finds potential duplicate tickets based on content hash.
    /// </summary>
    Task<IReadOnlyList<Ticket>> FindPotentialDuplicatesAsync(Ticket ticket);

    /// <summary>
    /// Gets all tickets in a group (parent + all children recursively).
    /// </summary>
    Task<IReadOnlyList<Ticket>> GetTicketGroupAsync(Ticket ticket);
}

/// <summary>
/// Exception thrown when ticket grouping operations fail.
/// </summary>
public class TicketGroupingException : Exception
{
    public TicketGroupingException(string message) : base(message)
    {
    }

    public TicketGroupingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
