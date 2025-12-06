using TicketMasala.Web.Services.Core;
using TicketMasala.Web.Services.Tickets;
using TicketMasala.Web.Services.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Services.Background;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TicketMasala.Web.Views.Shared.Components.Notification;
    public class NotificationViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Content(string.Empty);
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 5);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);

            ViewBag.UnreadCount = unreadCount;
            return View(notifications);
        }
}
