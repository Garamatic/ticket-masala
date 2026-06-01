namespace TicketMasala.Web.ViewComponents;

public class NotificationViewModel
{
    public List<TicketMasala.Domain.Entities.Notification> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
}
