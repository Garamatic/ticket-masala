using Microsoft.AspNetCore.Mvc;
using TicketMasala.Web.Engine.Core;

namespace TicketMasala.Web.ViewComponents;

public class NotificationViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationViewComponent> _logger;

    public NotificationViewComponent(INotificationService notificationService, ILogger<NotificationViewComponent> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = (User as System.Security.Claims.ClaimsPrincipal)?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return View("Default", new NotificationViewModel());
        }

        try
        {
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, count: 5);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);

            var model = new NotificationViewModel
            {
                Notifications = notifications,
                UnreadCount = unreadCount
            };

            return View("Default", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notifications for user {UserId}", userId);
            return View("Default", new NotificationViewModel());
        }
    }
}

public class NotificationViewModel
{
    public List<TicketMasala.Domain.Entities.Notification> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
}
