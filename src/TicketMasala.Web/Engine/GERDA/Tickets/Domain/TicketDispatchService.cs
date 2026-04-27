using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Enums;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Tickets.Domain
{
    /// <summary>
    /// Handles ticket dispatching logic (AI, manual, etc.)
    /// </summary>
    public class TicketDispatchService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ILogger<TicketDispatchService> _logger;

        public TicketDispatchService(ITicketRepository ticketRepository, ILogger<TicketDispatchService> logger)
        {
            _ticketRepository = ticketRepository;
            _logger = logger;
        }

        /// <summary>
        /// Assign a ticket to an agent (migrated from TicketService)
        /// </summary>
        public async Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId, IUserRepository userRepository, IEnumerable<ITicketObserver> observers, INotificationService notificationService, IAuditService auditService, IHttpContextAccessor httpContextAccessor)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: false);
            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketGuid} not found for assignment", ticketGuid);
                return false;
            }

            var agent = await userRepository.GetEmployeeByIdAsync(agentId);
            if (agent == null)
            {
                _logger.LogWarning("Agent {AgentId} not found", agentId);
                return false;
            }

            ticket.Responsible = agent;
            ticket.TicketStatus = Status.Assigned;

            // Add AI-Assigned tag if not present
            if (string.IsNullOrWhiteSpace(ticket.GerdaTags))
            {
                ticket.GerdaTags = "AI-Assigned";
            }
            else if (!ticket.GerdaTags.Contains("AI-Assigned"))
            {
                ticket.GerdaTags += ",AI-Assigned";
            }

            await _ticketRepository.UpdateAsync(ticket);

            _logger.LogInformation("Ticket {TicketGuid} assigned to agent {AgentId}", ticketGuid, agentId);

            // Notify observers
            foreach (var observer in observers)
            {
                try
                {
                    await observer.OnTicketAssignedAsync(ticket, agent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Observer {ObserverType} failed on ticket assignment", observer.GetType().Name);
                }
            }

            // Send notification to agent
            await notificationService.NotifyUserAsync(
                agentId,
                $"You have been assigned to ticket #{ticket.Guid.ToString()[..8]}",
                $"/Ticket/Detail/{ticket.Guid}",
                "Info"
            );

            // Audit Log
            var userId = httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await auditService.LogActionAsync(ticket.Guid, ActivityType.Assigned.GetDisplayText(), userId, "Responsible", null, agent.Name);

            return true;
        }
    }
}
