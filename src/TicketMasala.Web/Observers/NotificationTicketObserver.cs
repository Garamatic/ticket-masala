using TicketMasala.Web.Models;
using TicketMasala.Web.Services.Core;
using TicketMasala.Web.Services.Tickets;
using TicketMasala.Web.Services.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Services.Background;

namespace TicketMasala.Web.Observers;

public class NotificationTicketObserver : ITicketObserver
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationTicketObserver> _logger;

    public NotificationTicketObserver(
        INotificationService notificationService,
        ILogger<NotificationTicketObserver> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        // Could notify managers or team leads
        await Task.CompletedTask;
    }

    public async Task OnTicketAssignedAsync(Ticket ticket, Employee assignee)
    {
        // Already handled in TicketService.AssignTicketAsync for now, 
        // but ideally should move here.
        // For this refactor, we focus on comments and status updates.
        await Task.CompletedTask;
    }

    public async Task OnTicketCompletedAsync(Ticket ticket)
    {
        // Notify customer
        if (ticket.CustomerId != null)
        {
            await _notificationService.NotifyUserAsync(
                ticket.CustomerId,
                $"Ticket #{ticket.Guid.ToString().Substring(0, 8)} completed",
                $"/Ticket/Detail/{ticket.Guid}",
                "Success"
            );
        }
    }

    public async Task OnTicketUpdatedAsync(Ticket ticket)
    {
        // Notify customer on status change (moved from TicketService)
        // Note: We need to know if status changed. 
        // Since we don't have old state here easily, we might rely on the fact 
        // that TicketService calls this after update.
        // However, TicketService had specific logic for status change.
        // For now, let's just notify on any update if we want, or skip if we can't detect change.
        // The previous logic in TicketService was:
        /*
        if (ticket.CustomerId != null)
        {
            await _notificationService.NotifyUserAsync(
                ticket.CustomerId, 
                $"Ticket #{ticket.Guid.ToString().Substring(0, 8)} status changed to {ticket.TicketStatus}", 
                $"/Ticket/Detail/{ticket.Guid}", 
                "Info"
            );
        }
        */
        // This is a bit spammy if it notifies on EVERY update (e.g. description change).
        // Ideally we'd pass the change type or old state.
        // For this refactor, let's implement comment notifications first, 
        // and leave status update notifications in TicketService or move them carefully.
        // The architecture review asked for "Observer Pattern for Comments".
        
        await Task.CompletedTask;
    }

    public async Task OnTicketCommentedAsync(TicketComment comment)
    {
        try
        {
            var ticket = comment.Ticket;
            if (ticket == null) return;

            if (!comment.IsInternal)
            {
                // If author is customer -> notify responsible agent
                // If author is employee -> notify customer
                
                if (ticket.CustomerId != null && ticket.CustomerId != comment.AuthorId)
                {
                    await _notificationService.NotifyUserAsync(
                        ticket.CustomerId,
                        $"New reply on ticket #{ticket.Guid.ToString().Substring(0, 8)}",
                        $"/Ticket/Detail/{ticket.Guid}",
                        "Reply"
                    );
                }

                if (ticket.ResponsibleId != null && ticket.ResponsibleId != comment.AuthorId)
                {
                    await _notificationService.NotifyUserAsync(
                        ticket.ResponsibleId,
                        $"New reply on ticket #{ticket.Guid.ToString().Substring(0, 8)}",
                        $"/Ticket/Detail/{ticket.Guid}",
                        "Reply"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send comment notification for ticket {TicketId}", comment.TicketId);
        }
    }

}
