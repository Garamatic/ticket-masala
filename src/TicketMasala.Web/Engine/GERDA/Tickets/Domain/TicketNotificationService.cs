using TicketMasala.Web.Engine.Core;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Repositories;
using TicketMasala.Domain.Common;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TicketMasala.Web.Engine.GERDA.Tickets.Domain
{
    /// <summary>
    /// Handles ticket notification logic (user alerts, reminders, etc.)
    /// </summary>
    public class TicketNotificationService
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<TicketNotificationService> _logger;

        public TicketNotificationService(INotificationService notificationService, ILogger<TicketNotificationService> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // Notify responsible and customer of ticket status change (migrated from TicketService)
        public async Task NotifyStatusChangeAsync(Ticket ticket)
        {
            if (ticket.ResponsibleId != null && (ticket.TicketStatus == Status.Completed || ticket.TicketStatus == Status.Rejected))
            {
                await _notificationService.NotifyUserAsync(
                    ticket.ResponsibleId,
                    $"Ticket #{ticket.Guid.ToString().Substring(0, 8)} status changed to {ticket.TicketStatus}",
                    $"/Ticket/Detail/{ticket.Guid}",
                    "Info"
                );
            }
            if (ticket.CustomerId != null)
            {
                await _notificationService.NotifyUserAsync(
                    ticket.CustomerId,
                    $"Ticket #{ticket.Guid.ToString().Substring(0, 8)} status changed to {ticket.TicketStatus}",
                    $"/Ticket/Detail/{ticket.Guid}",
                    "Info"
                );
            }
        }

        // Example: Notify user of ticket assignment
        public async Task NotifyAssignmentAsync(string userId, Guid ticketGuid)
        {
            await _notificationService.NotifyUserAsync(
                userId,
                $"You have been assigned to ticket #{ticketGuid.ToString().Substring(0, 8)}",
                $"/Ticket/Detail/{ticketGuid}",
                "Info"
            );
            _logger.LogInformation("User {UserId} notified of assignment to ticket {TicketGuid}", userId, ticketGuid);
        }
    }
}
