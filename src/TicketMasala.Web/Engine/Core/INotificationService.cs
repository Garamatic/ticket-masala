using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.Core;

public interface INotificationService
{
    Task NotifyUserAsync(string userId, string message, string? linkUrl = null, string type = "Info");
    Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 10);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(string userId);
}
