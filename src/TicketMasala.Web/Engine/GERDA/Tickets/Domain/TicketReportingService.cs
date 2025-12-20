using TicketMasala.Web.Models;
using TicketMasala.Web.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TicketMasala.Web.Engine.GERDA.Tickets.Domain
{
    /// <summary>
    /// Handles ticket reporting logic (analytics, summaries, etc.)
    /// </summary>
    public class TicketReportingService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ILogger<TicketReportingService> _logger;

        public TicketReportingService(ITicketRepository ticketRepository, ILogger<TicketReportingService> logger)
        {
            _ticketRepository = ticketRepository;
            _logger = logger;
        }

        // Get current workload for an employee (migrated from TicketService)
        public async Task<int> GetEmployeeCurrentWorkloadAsync(string agentId)
        {
            var tickets = await _ticketRepository.GetByResponsibleIdAsync(agentId);
            var activeTickets = tickets.Where(t =>
                t.TicketStatus == Status.Assigned ||
                t.TicketStatus == Status.InProgress);
            return activeTickets.Sum(t => t.EstimatedEffortPoints);
        }

        // Example: Get ticket count by status
        public async Task<Dictionary<string, int>> GetTicketCountByStatusAsync()
        {
            var tickets = await _ticketRepository.GetAllAsync();
            var result = new Dictionary<string, int>();
            foreach (var t in tickets)
            {
                var status = t.TicketStatus.ToString();
                if (!result.ContainsKey(status)) result[status] = 0;
                result[status]++;
            }
            return result;
        }
    }
}
