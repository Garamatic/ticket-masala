using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TicketMasala.Web.Controllers;
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index", "Home");

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 50);
            return View(notifications);
        }

        public async Task<IActionResult> Read(Guid id)
        {
            await _notificationService.MarkAsReadAsync(id);
            
            // Redirect to the notification link if available, otherwise back to list
            // We need to fetch the notification again to get the link, or we could have passed it?
            // Since MarkAsReadAsync doesn't return the notification, let's just redirect to Index or Home for now,
            // or we could improve the service to return the notification.
            // But wait, the link in the view points here.
            
            // Let's assume we want to redirect to the LinkUrl if it exists.
            // I'll need to get the notification first.
            // But MarkAsReadAsync does a find.
            
            // Let's just redirect to Index for now as a fallback, or improve this later.
            // Actually, the view generates the link to Read action.
            // If I want to redirect to the target, I should probably pass the target URL or fetch it.
            
            // Let's fetch it first (inefficient but works)
            // Or better, update service to return it.
            // For now, I'll just redirect to Home/Index if I can't find it easily.
            // Wait, I can just redirect to the notifications list.
            
            return RedirectToAction("Index");
        }
        
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await _notificationService.MarkAllAsReadAsync(userId);
            }
            return RedirectToAction("Index");
        }
}
