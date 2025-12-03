using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Services
{
    /// <summary>
    /// Service responsible for ticket business logic.
    /// Follows Information Expert and Single Responsibility principles.
    /// </summary>
    public interface ITicketService
    {
        Task<List<SelectListItem>> GetCustomerSelectListAsync();
        Task<List<SelectListItem>> GetEmployeeSelectListAsync();
        Task<List<SelectListItem>> GetProjectSelectListAsync();
        Task<Ticket> CreateTicketAsync(string description, string customerId, string? responsibleId, Guid? projectGuid, DateTime? completionTarget);
        Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketGuid);
        Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId);
    }

    public class TicketService : ITicketService
    {
        private readonly ITProjectDB _context;
        private readonly ILogger<TicketService> _logger;

        public TicketService(ITProjectDB context, ILogger<TicketService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get customer dropdown list
        /// </summary>
        public async Task<List<SelectListItem>> GetCustomerSelectListAsync()
        {
            var customers = await _context.Customers.ToListAsync();
            return customers.Select(c => new SelectListItem
            {
                Value = c.Id,
                Text = $"{c.FirstName} {c.LastName}"
            }).ToList();
        }

        /// <summary>
        /// Get employee dropdown list
        /// </summary>
        public async Task<List<SelectListItem>> GetEmployeeSelectListAsync()
        {
            var employees = await _context.Employees.ToListAsync();
            return employees.Select(e => new SelectListItem
            {
                Value = e.Id,
                Text = $"{e.FirstName} {e.LastName}"
            }).ToList();
        }

        /// <summary>
        /// Get project dropdown list
        /// </summary>
        public async Task<List<SelectListItem>> GetProjectSelectListAsync()
        {
            var projects = await _context.Projects.ToListAsync();
            return projects.Select(p => new SelectListItem
            {
                Value = p.Guid.ToString(),
                Text = p.Name
            }).ToList();
        }

        /// <summary>
        /// Create a new ticket with proper defaults and associations
        /// </summary>
        public async Task<Ticket> CreateTicketAsync(
            string description, 
            string customerId, 
            string? responsibleId, 
            Guid? projectGuid, 
            DateTime? completionTarget)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                throw new ArgumentException("Customer not found", nameof(customerId));
            }

            Employee? responsible = null;
            if (!string.IsNullOrWhiteSpace(responsibleId))
            {
                responsible = await _context.Employees.FindAsync(responsibleId);
            }

            var ticket = new Ticket
            {
                Description = description,
                Customer = customer,
                Responsible = responsible,
                TicketStatus = responsible != null ? Status.Assigned : Status.Pending,
                TicketType = TicketType.ProjectRequest,
                CompletionTarget = completionTarget ?? DateTime.UtcNow.AddDays(14),
                CreatorGuid = Guid.Parse(customer.Id),
                Comments = new List<string>()
            };

            _context.Tickets.Add(ticket);

            // If a project is selected, add the ticket to that project
            if (projectGuid.HasValue && projectGuid.Value != Guid.Empty)
            {
                var project = await _context.Projects
                    .Include(p => p.Tasks)
                    .FirstOrDefaultAsync(p => p.Guid == projectGuid.Value);
                
                if (project != null)
                {
                    project.Tasks.Add(ticket);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Ticket {TicketGuid} created successfully", ticket.Guid);
            return ticket;
        }

        /// <summary>
        /// Get detailed ticket information with GERDA insights
        /// </summary>
        public async Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketGuid)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Responsible)
                .Include(t => t.ParentTicket)
                .Include(t => t.SubTickets)
                .FirstOrDefaultAsync(t => t.Guid == ticketGuid);

            if (ticket == null)
            {
                return null;
            }

            var project = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Tasks.Any(t => t.Guid == ticketGuid));

            var viewModel = new TicketDetailsViewModel
            {
                Guid = ticket.Guid,
                Description = ticket.Description,
                TicketStatus = ticket.TicketStatus,
                TicketType = ticket.TicketType,
                CreationDate = ticket.CreationDate,
                CompletionTarget = ticket.CompletionTarget,
                CompletionDate = ticket.CompletionDate,
                Comments = ticket.Comments,
                
                // Relationships
                ResponsibleName = ticket.Responsible != null
                    ? $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}"
                    : null,
                ResponsibleId = ticket.Responsible?.Id,
                CustomerName = ticket.Customer != null
                    ? $"{ticket.Customer.FirstName} {ticket.Customer.LastName}"
                    : null,
                CustomerId = ticket.Customer?.Id,
                ParentTicketGuid = ticket.ParentTicket?.Guid,
                ProjectGuid = project?.Guid,
                ProjectName = project?.Name,
                
                SubTickets = ticket.SubTickets.Select(st => new SubTicketInfo
                {
                    Guid = st.Guid,
                    Description = st.Description,
                    TicketStatus = st.TicketStatus
                }).ToList(),
                
                // GERDA AI Insights
                EstimatedEffortPoints = ticket.EstimatedEffortPoints,
                PriorityScore = ticket.PriorityScore,
                GerdaTags = ticket.GerdaTags
            };

            return viewModel;
        }

        /// <summary>
        /// Assign a ticket to an agent
        /// </summary>
        public async Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == ticketGuid);
            
            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketGuid} not found for assignment", ticketGuid);
                return false;
            }

            var agent = await _context.Employees.FindAsync(agentId);
            
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

            await _context.SaveChangesAsync();

            _logger.LogInformation("Ticket {TicketGuid} assigned to agent {AgentId}", ticketGuid, agentId);
            return true;
        }
    }
}
