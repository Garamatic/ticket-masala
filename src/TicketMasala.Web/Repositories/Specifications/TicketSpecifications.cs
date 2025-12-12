using TicketMasala.Web.Models;

namespace TicketMasala.Web.Repositories.Specifications;

/// <summary>
/// Specification for tickets by status.
/// </summary>
public class TicketsByStatusSpecification : Specification<Ticket>
{
    public TicketsByStatusSpecification(Status status)
    {
        SetCriteria(t => t.TicketStatus == status);
    }
}

/// <summary>
/// Specification for tickets by multiple statuses.
/// </summary>
public class TicketsByStatusesSpecification : Specification<Ticket>
{
    public TicketsByStatusesSpecification(params Status[] statuses)
    {
        SetCriteria(t => statuses.Contains(t.TicketStatus));
    }
}

/// <summary>
/// Specification for unassigned tickets.
/// </summary>
public class UnassignedTicketsSpecification : Specification<Ticket>
{
    public UnassignedTicketsSpecification()
    {
        SetCriteria(t => t.ResponsibleId == null);
        AddInclude(t => t.Customer!);
    }
}

/// <summary>
/// Specification for tickets assigned to a specific agent.
/// </summary>
public class TicketsByResponsibleSpecification : Specification<Ticket>
{
    public TicketsByResponsibleSpecification(string responsibleId)
    {
        SetCriteria(t => t.ResponsibleId == responsibleId);
        AddInclude(t => t.Customer!);
        AddInclude(t => t.Responsible!);
    }
}

/// <summary>
/// Specification for tickets by customer.
/// </summary>
public class TicketsByCustomerSpecification : Specification<Ticket>
{
    public TicketsByCustomerSpecification(string customerId)
    {
        SetCriteria(t => t.CustomerId == customerId);
        AddInclude(t => t.Responsible!);
    }
}

/// <summary>
/// Specification for open/active tickets (not completed or failed).
/// </summary>
public class OpenTicketsSpecification : Specification<Ticket>
{
    public OpenTicketsSpecification()
    {
        SetCriteria(t => t.TicketStatus != Status.Completed &&
                        t.TicketStatus != Status.Failed &&
                        t.ValidUntil == null);
        ApplyOrderByDescending(t => t.PriorityScore);
    }
}

/// <summary>
/// Specification for tickets in a specific project.
/// </summary>
public class TicketsByProjectSpecification : Specification<Ticket>
{
    public TicketsByProjectSpecification(Guid projectGuid)
    {
        SetCriteria(t => t.ProjectGuid == projectGuid);
        AddInclude(t => t.Customer!);
        AddInclude(t => t.Responsible!);
    }
}

/// <summary>
/// Specification for overdue tickets (past completion target).
/// </summary>
public class OverdueTicketsSpecification : Specification<Ticket>
{
    public OverdueTicketsSpecification()
    {
        var now = DateTime.UtcNow;
        SetCriteria(t => t.CompletionTarget < now &&
                        t.TicketStatus != Status.Completed &&
                        t.ValidUntil == null);
        ApplyOrderBy(t => t.CompletionTarget!);
    }
}

/// <summary>
/// Specification for tickets with high priority (above threshold).
/// </summary>
public class HighPriorityTicketsSpecification : Specification<Ticket>
{
    public HighPriorityTicketsSpecification(int priorityThreshold = 50)
    {
        SetCriteria(t => t.PriorityScore >= priorityThreshold &&
                        t.ValidUntil == null);
        ApplyOrderByDescending(t => t.PriorityScore);
    }
}

/// <summary>
/// Specification for tickets with pagination.
/// </summary>
public class PaginatedTicketsSpecification : Specification<Ticket>
{
    public PaginatedTicketsSpecification(int page, int pageSize)
    {
        SetCriteria(t => t.ValidUntil == null);
        ApplyOrderByDescending(t => t.CreationDate);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AddInclude(t => t.Customer!);
        AddInclude(t => t.Responsible!);
    }

}
