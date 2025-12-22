using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;

namespace TicketMasala.Web.Engine.Core;

using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<NotificationService> _logger;
    private readonly IEmailService _emailService;

    public NotificationService(
        MasalaDbContext context,
        ILogger<NotificationService> logger,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task NotifyUserAsync(string userId, string message, string? linkUrl = null, string type = "Info")
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Message = message,
                LinkUrl = linkUrl,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send email notification
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var emailSubject = $"Ticket Masala: {type}";
                    var emailBody = $@"
                        <h2>{type} Notification</h2>
                        <p>{message}</p>
                        {(string.IsNullOrEmpty(linkUrl) ? "" : $"<p><a href='{linkUrl}'>View Details</a></p>")}
                        <hr/>
                        <p style='color: #666; font-size: 12px;'>This is an automated notification from Ticket Masala.</p>
                    ";

                    await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send email notification to user {UserId}", userId);
                // Continue - email failure should not break notification creation
            }

            _logger.LogInformation("Notification sent to user {UserId}: {Message}", userId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
        }
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 10)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
