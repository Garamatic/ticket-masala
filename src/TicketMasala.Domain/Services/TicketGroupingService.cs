using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Exceptions;

namespace TicketMasala.Domain.Services;

/// <summary>
/// Implementation of ticket grouping domain logic.
/// </summary>
public class TicketGroupingService : ITicketGroupingService
{
    public Task GroupTicketsAsync(
        Ticket parentTicket,
        IEnumerable<Ticket> childTickets,
        string groupedByUserId)
    {
        // Validate parent ticket can have children
        if (parentTicket.ParentTicketGuid.HasValue)
        {
            throw new TicketGroupingException(
                "Cannot group tickets under a child ticket. " +
                "The parent ticket must not have its own parent.");
        }

        // Prevent grouping a ticket as its own child
        var childList = childTickets.ToList();
        if (childList.Any(c => c.Guid == parentTicket.Guid))
        {
            throw new TicketGroupingException("A ticket cannot be grouped under itself.");
        }

        // Validate child tickets are not already parents of other tickets
        // (to prevent deep nesting - we only support one level of grouping)
        var parentsInChildren = childList.Where(c => c.SubTickets.Any()).ToList();
        if (parentsInChildren.Any())
        {
            throw new TicketGroupingException(
                $"Cannot group tickets that already have children: " +
                $"{string.Join(", ", parentsInChildren.Select(c => c.Guid))}");
        }

        // Validate child tickets are not already children of other parents
        var alreadyChildren = childList.Where(c => c.ParentTicketGuid.HasValue).ToList();
        if (alreadyChildren.Any())
        {
            throw new TicketGroupingException(
                $"Cannot group tickets that are already children of another ticket: " +
                $"{string.Join(", ", alreadyChildren.Select(c => c.Guid))}");
        }

        // Perform the grouping
        foreach (var child in childList)
        {
            parentTicket.AddSubTicket(child);
        }

        // Raise domain event for audit trail
        parentTicket.RecordChildrenGrouped(childList, groupedByUserId);

        return Task.CompletedTask;
    }

    public Task UngroupTicketAsync(Ticket childTicket, string ungroupedByUserId)
    {
        var formerParentGuid = childTicket.ParentTicketGuid;
        if (!formerParentGuid.HasValue)
        {
            // Not grouped - idempotent
            return Task.CompletedTask;
        }

        // Clear the parent relationship
        childTicket.SetParentTicket(null);

        // Raise domain event for audit trail
        childTicket.RecordUngrouped(formerParentGuid, ungroupedByUserId);

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Ticket>> SplitTicketAsync(
        Ticket parentTicket,
        IEnumerable<string> splitDescriptions,
        string splitByUserId)
    {
        var descriptions = splitDescriptions.ToList();

        if (descriptions.Count < 2)
        {
            throw new TicketGroupingException(
                "Cannot split a ticket into fewer than 2 child tickets. " +
                "Use 'Edit' instead to modify the existing ticket.");
        }

        // Validate the parent ticket can be edited
        if (!parentTicket.CanEditInCurrentState())
        {
            throw new DomainRuleException(
                $"Cannot split ticket in {parentTicket.TicketStatus} status. " +
                "Ticket must be editable.");
        }

        var childTickets = new List<Ticket>();

        foreach (var description in descriptions)
        {
            var childTicket = Ticket.Create(
                description,
                $"Split from: {parentTicket.Title}",
                parentTicket.CustomerId,
                parentTicket.DomainId,
                parentTicket.ProjectGuid,
                parentTicket.WorkItemTypeCode);

            // Copy relevant properties from parent
            childTicket.SetPropertyForSeeding(t =>
            {
                t.PriorityScore = parentTicket.PriorityScore;
                t.DomainCustomFieldsJson = parentTicket.DomainCustomFieldsJson;
                t.ConfigVersionId = parentTicket.ConfigVersionId;
            });

            childTickets.Add(childTicket);
        }

        // Group all children under the parent
        await GroupTicketsAsync(parentTicket, childTickets, splitByUserId);

        return childTickets;
    }

    public async Task<Ticket> MergeTicketsAsync(
        IEnumerable<Ticket> ticketsToMerge,
        string mergedByUserId)
    {
        var tickets = ticketsToMerge.ToList();

        if (tickets.Count < 2)
        {
            throw new TicketGroupingException(
                "Cannot merge fewer than 2 tickets. " +
                "Merging requires at least 2 tickets.");
        }

        // Use the first ticket as the parent (merged ticket)
        var parentTicket = tickets.First();
        var childrenToGroup = tickets.Skip(1).ToList();

        // Validate all tickets are editable
        var nonEditableTickets = tickets.Where(t => !t.CanEditInCurrentState()).ToList();
        if (nonEditableTickets.Any())
        {
            throw new DomainRuleException(
                $"Cannot merge tickets in non-editable status. " +
                $"Non-editable tickets: {string.Join(", ", nonEditableTickets.Select(t => t.Guid))}");
        }

        // Update the parent ticket description to indicate merge
        var mergedDescription = $"Merged ticket containing:\n" +
            string.Join("\n", tickets.Select(t => $"- {t.Title}: {t.Description}"));

        parentTicket.UpdateDescription(mergedDescription, mergedByUserId);

        // Group other tickets as children
        await GroupTicketsAsync(parentTicket, childrenToGroup, mergedByUserId);

        // Mark children as cancelled/merged
        foreach (var child in childrenToGroup)
        {
            child.TransitionTo(Status.Cancelled, mergedByUserId);
        }

        return parentTicket;
    }

    public Task<IReadOnlyList<Ticket>> FindPotentialDuplicatesAsync(Ticket ticket)
    {
        // This would typically query a repository
        // For now, return empty list as the service interface contract
        return Task.FromResult<IReadOnlyList<Ticket>>(new List<Ticket>());
    }

    public Task<IReadOnlyList<Ticket>> GetTicketGroupAsync(Ticket ticket)
    {
        var group = new List<Ticket>();

        // If this is a child ticket, find the root parent
        var rootTicket = ticket;
        while (rootTicket.ParentTicket != null)
        {
            rootTicket = rootTicket.ParentTicket;
        }

        // Add the root
        group.Add(rootTicket);

        // Add all children recursively
        void AddChildrenRecursive(Ticket parent)
        {
            foreach (var child in parent.SubTickets)
            {
                group.Add(child);
                AddChildrenRecursive(child);
            }
        }

        AddChildrenRecursive(rootTicket);

        return Task.FromResult<IReadOnlyList<Ticket>>(group);
    }
}
