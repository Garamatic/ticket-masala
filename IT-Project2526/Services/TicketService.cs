using IT_Project2526.Models;
using IT_Project2526.Repositories;
using IT_Project2526.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace IT_Project2526.Services
{
    /// <summary>
    /// Service implementation for ticket business logic
    /// </summary>
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<TicketService> _logger;

        public TicketService(
            ITicketRepository ticketRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<TicketService> logger)
        {
            _ticketRepository = ticketRepository;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync()
        {
            try
            {
                var tickets = await _ticketRepository.GetAllWithDetailsAsync();
                return MapToViewModels(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tickets");
                throw;
            }
        }

        public async Task<TicketViewModel?> GetTicketByIdAsync(Guid id)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdWithDetailsAsync(id);
                return ticket == null ? null : MapToViewModel(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ticket {TicketId}", id);
                throw;
            }
        }

        public async Task<Guid> CreateTicketAsync(TicketViewModel model, string currentUserId)
        {
            try
            {
                _logger.LogInformation("Creating new ticket by user {UserId}", currentUserId);

                // TODO: Map from TicketViewModel to Ticket entity
                // This is placeholder - you'll need to add customer lookup
                throw new NotImplementedException("Ticket creation needs customer assignment logic");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket");
                throw;
            }
        }

        public async Task UpdateTicketAsync(Guid id, TicketViewModel model)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(id)
                    ?? throw new InvalidOperationException($"Ticket not found: {id}");

                ticket.Description = model.Description;
                // Update other properties as needed

                await _ticketRepository.UpdateAsync(ticket);
                await _ticketRepository.SaveChangesAsync();

                _logger.LogInformation("Ticket updated: {TicketId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket {TicketId}", id);
                throw;
            }
        }

        public async Task DeleteTicketAsync(Guid id)
        {
            try
            {
                await _ticketRepository.DeleteAsync(id);
                await _ticketRepository.SaveChangesAsync();

                _logger.LogInformation("Ticket soft deleted: {TicketId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ticket {TicketId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<TicketViewModel>> GetProjectTicketsAsync(Guid projectId)
        {
            try
            {
                var tickets = await _ticketRepository.GetByProjectIdAsync(projectId);
                return MapToViewModels(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tickets for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<IEnumerable<TicketViewModel>> GetCustomerTicketsAsync(string customerId)
        {
            try
            {
                var tickets = await _ticketRepository.GetByCustomerIdAsync(customerId);
                return MapToViewModels(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tickets for customer {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<IEnumerable<TicketViewModel>> GetUserTicketsAsync(string userId)
        {
            try
            {
                var tickets = await _ticketRepository.GetByResponsibleIdAsync(userId);
                return MapToViewModels(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tickets for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<TicketViewModel>> GetWatchedTicketsAsync(string userId)
        {
            try
            {
                var tickets = await _ticketRepository.GetWatchedByUserAsync(userId);
                return MapToViewModels(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving watched tickets for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<TicketViewModel>> GetOverdueTicketsAsync()
        {
            try
            {
                var tickets = await _ticketRepository.GetOverdueTicketsAsync();
                return MapToViewModels(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue tickets");
                throw;
            }
        }

        public async Task AssignTicketAsync(Guid ticketId, string userId)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(ticketId)
                    ?? throw new InvalidOperationException($"Ticket not found: {ticketId}");

                var user = await _userManager.FindByIdAsync(userId)
                    ?? throw new InvalidOperationException($"User not found: {userId}");

                ticket.Responsible = user;
                ticket.TicketStatus = Status.Assigned;

                await _ticketRepository.UpdateAsync(ticket);
                await _ticketRepository.SaveChangesAsync();

                _logger.LogInformation("Ticket {TicketId} assigned to user {UserId}", ticketId, userId);

                // Send notification
                await _emailService.SendTicketNotificationAsync(user.Email!, ticket.Description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning ticket {TicketId} to user {UserId}", ticketId, userId);
                throw;
            }
        }

        public async Task UpdateTicketStatusAsync(Guid ticketId, Status newStatus)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(ticketId)
                    ?? throw new InvalidOperationException($"Ticket not found: {ticketId}");

                var oldStatus = ticket.TicketStatus;
                ticket.TicketStatus = newStatus;

                if (newStatus == Status.Completed)
                {
                    ticket.CompletionDate = DateTime.UtcNow;
                }

                await _ticketRepository.UpdateAsync(ticket);
                await _ticketRepository.SaveChangesAsync();

                _logger.LogInformation("Ticket {TicketId} status updated from {OldStatus} to {NewStatus}", 
                    ticketId, oldStatus, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket status {TicketId}", ticketId);
                throw;
            }
        }

        public async Task AddCommentAsync(Guid ticketId, string comment, string userId)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(ticketId)
                    ?? throw new InvalidOperationException($"Ticket not found: {ticketId}");

                var user = await _userManager.FindByIdAsync(userId);
                var userName = user?.Name ?? "Unknown";

                var commentWithMeta = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {userName}: {comment}";
                ticket.Comments.Add(commentWithMeta);

                await _ticketRepository.UpdateAsync(ticket);
                await _ticketRepository.SaveChangesAsync();

                _logger.LogInformation("Comment added to ticket {TicketId} by user {UserId}", ticketId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to ticket {TicketId}", ticketId);
                throw;
            }
        }

        public async Task AddWatcherAsync(Guid ticketId, string userId)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdWithDetailsAsync(ticketId)
                    ?? throw new InvalidOperationException($"Ticket not found: {ticketId}");

                var user = await _userManager.FindByIdAsync(userId)
                    ?? throw new InvalidOperationException($"User not found: {userId}");

                if (!ticket.Watchers.Any(w => w.Id == userId))
                {
                    ticket.Watchers.Add(user);
                    await _ticketRepository.UpdateAsync(ticket);
                    await _ticketRepository.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} added as watcher to ticket {TicketId}", userId, ticketId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding watcher to ticket {TicketId}", ticketId);
                throw;
            }
        }

        public async Task RemoveWatcherAsync(Guid ticketId, string userId)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdWithDetailsAsync(ticketId)
                    ?? throw new InvalidOperationException($"Ticket not found: {ticketId}");

                var watcher = ticket.Watchers.FirstOrDefault(w => w.Id == userId);
                if (watcher != null)
                {
                    ticket.Watchers.Remove(watcher);
                    await _ticketRepository.UpdateAsync(ticket);
                    await _ticketRepository.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} removed as watcher from ticket {TicketId}", userId, ticketId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing watcher from ticket {TicketId}", ticketId);
                throw;
            }
        }

        // Private helper methods

        private TicketViewModel MapToViewModel(Ticket ticket)
        {
            return new TicketViewModel
            {
                Guid = ticket.Guid,
                Description = ticket.Description,
                Status = ticket.TicketStatus.ToString(),
                ResponsibleName = ticket.Responsible?.Name,
                CommentsCount = ticket.Comments?.Count ?? 0,
                CompletionTarget = ticket.CompletionTarget
            };
        }

        private IEnumerable<TicketViewModel> MapToViewModels(IEnumerable<Ticket> tickets)
        {
            return tickets.Select(MapToViewModel);
        }
    }
}
