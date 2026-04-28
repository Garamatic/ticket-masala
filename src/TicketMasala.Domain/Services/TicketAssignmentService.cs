using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Exceptions;

namespace TicketMasala.Domain.Services;

/// <summary>
/// Implementation of ticket assignment domain logic.
/// </summary>
public class TicketAssignmentService : ITicketAssignmentService
{
    public Task<bool> AssignToEmployeeAsync(
        Ticket ticket,
        Employee employee,
        string assignedByUserId,
        IEnumerable<string> assignedByRoles)
    {
        // Validate using domain rules
        ticket.ValidateCanAssign(assignedByUserId, assignedByRoles);

        // Check if ticket can be assigned in current state
        if (!ticket.CanBeAssigned())
        {
            throw new DomainRuleException(
                $"Cannot assign ticket in {ticket.TicketStatus} status. " +
                "Ticket must be in Pending, Assigned, or InProgress status.");
        }

        // Perform the assignment using rich domain method
        ticket.AssignTo(employee.Id, assignedByUserId);

        // Set the navigation property
        ticket.SetResponsible(employee);

        return Task.FromResult(true);
    }

    public Task<bool> UnassignAsync(
        Ticket ticket,
        string unassignedByUserId,
        IEnumerable<string> unassignedByRoles)
    {
        // Validate authorization
        ticket.ValidateCanAssign(unassignedByUserId, unassignedByRoles);

        if (string.IsNullOrEmpty(ticket.ResponsibleId))
        {
            // Already unassigned - idempotent
            return Task.FromResult(true);
        }

        ticket.Unassign(unassignedByUserId);
        ticket.SetResponsible(null);

        return Task.FromResult(true);
    }

    public bool ShouldAutoDispatch(Ticket ticket)
    {
        // Auto-dispatch criteria:
        // 1. Ticket is in Pending status
        // 2. No responsible assigned yet
        // 3. Ticket is not a child ticket (child tickets follow parent assignment)
        return ticket.TicketStatus == Status.Pending &&
               string.IsNullOrEmpty(ticket.ResponsibleId) &&
               !ticket.ParentTicketGuid.HasValue;
    }

    public Task<AssignmentRecommendation> GetRecommendationsAsync(Ticket ticket)
    {
        // This is a simplified implementation
        // In production, this would integrate with GERDA ML models

        var recommendation = new AssignmentRecommendation
        {
            // No recommendation without external data (employees, workloads)
            // This is a placeholder for the domain service contract
            Explanation = "Recommendation requires external domain configuration data"
        };

        return Task.FromResult(recommendation);
    }
}
