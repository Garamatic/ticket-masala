using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using IT_Project2526.Repositories;
using IT_Project2526.Observers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Services
{
    /// <summary>
    /// Service responsible for ticket business logic.
    /// Follows Information Expert and Single Responsibility principles.
    /// Updated to use Repository pattern and Observer pattern.
    /// </summary>
    public interface ITicketService
    {
        Task<List<SelectListItem>> GetCustomerSelectListAsync();
        Task<List<SelectListItem>> GetEmployeeSelectListAsync();
        Task<List<SelectListItem>> GetProjectSelectListAsync();
        Task<Guid> GetCurrentUserDepartmentIdAsync();
        Task<Ticket> CreateTicketAsync(string description, string customerId, string? responsibleId, Guid? projectGuid, DateTime? completionTarget);
        Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketGuid);
        Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId);
        Task<bool> AssignTicketWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid);
        Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync();
        Task<Employee?> GetEmployeeByIdAsync(string agentId);
        Task<int> GetEmployeeCurrentWorkloadAsync(string agentId);
        Task<Ticket?> GetTicketForEditAsync(Guid ticketGuid);
        Task<List<SelectListItem>> GetAllUsersSelectListAsync();
        Task<bool> UpdateTicketAsync(Ticket ticket);
        Task<BatchAssignResult> BatchAssignTicketsAsync(BatchAssignRequest request, Func<Guid, Task<string?>> getRecommendedAgent);
        Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel);
        Task BatchAssignToAgentAsync(List<Guid> ticketIds, string agentId);
        Task BatchUpdateStatusAsync(List<Guid> ticketIds, Status status);
    }

    public class TicketService : ITicketService
    {
        private readonly ITProjectDB _context;
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IEnumerable<ITicketObserver> _observers;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TicketService> _logger;

        public TicketService(
            ITProjectDB context, 
            ITicketRepository ticketRepository,
            IUserRepository userRepository,
            IProjectRepository projectRepository,
            IEnumerable<ITicketObserver> observers,
            INotificationService notificationService,
            IAuditService auditService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TicketService> logger)
        {
            _context = context;
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
            _projectRepository = projectRepository;
            _observers = observers;
            _notificationService = notificationService;
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "System";
        }

        public async Task<Guid> GetCurrentUserDepartmentIdAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Guid.Empty;

            var user = await _context.Users.FindAsync(userId);
            var employee = user as Employee;
            return employee?.DepartmentId ?? Guid.Empty;
        }

        /// <summary>
        /// Get customer dropdown list
        /// </summary>
        public async Task<List<SelectListItem>> GetCustomerSelectListAsync()
        {
            var customers = await _userRepository.GetAllCustomersAsync();
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
            var employees = await _userRepository.GetAllEmployeesAsync();
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
            var projects = await _projectRepository.GetAllAsync();
            return projects.Select(p => new SelectListItem
            {
                Value = p.Guid.ToString(),
                Text = p.Name
            }).ToList();
        }
        
        /// <summary>
        /// Get all tickets with customer and responsible information
        /// </summary>
        public async Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync()
        {
            var departmentId = await GetCurrentUserDepartmentIdAsync();
            var tickets = await _ticketRepository.GetAllAsync(departmentId);
            
            return tickets.Select(t => new TicketViewModel
            {
                Guid = t.Guid,
                Description = t.Description,
                TicketStatus = t.TicketStatus,
                CreationDate = t.CreationDate,
                CompletionTarget = t.CompletionTarget,
                ResponsibleName = t.Responsible != null
                    ? $"{t.Responsible.FirstName} {t.Responsible.LastName}"
                    : "Not Assigned",
                CustomerName = t.Customer != null
                    ? $"{t.Customer.FirstName} {t.Customer.LastName}"
                    : "Unknown"
            }).ToList();
        }

        /// <summary>
        /// Create a new ticket with proper defaults and associations
        /// Notifies observers after creation (triggers GERDA processing)
        /// </summary>
        public async Task<Ticket> CreateTicketAsync(
            string description, 
            string customerId, 
            string? responsibleId, 
            Guid? projectGuid, 
            DateTime? completionTarget)
        {
            var customer = await _userRepository.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                throw new ArgumentException("Customer not found", nameof(customerId));
            }

            Employee? responsible = null;
            if (!string.IsNullOrWhiteSpace(responsibleId))
            {
                responsible = await _userRepository.GetEmployeeByIdAsync(responsibleId);
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
                Comments = new List<TicketComment>()
            };

            // Add ticket via repository
            await _ticketRepository.AddAsync(ticket);

            // If a project is selected, add the ticket to that project
            if (projectGuid.HasValue && projectGuid.Value != Guid.Empty)
            {
                var project = await _projectRepository.GetByIdAsync(projectGuid.Value, includeRelations: true);
                
                if (project != null)
                {
                    project.Tasks.Add(ticket);
                    await _projectRepository.UpdateAsync(project);
                }
            }

            _logger.LogInformation("Ticket {TicketGuid} created successfully", ticket.Guid);
            
            // Notify observers (triggers GERDA processing automatically)
            await NotifyObserversCreatedAsync(ticket);

            // Audit Log
            await _auditService.LogActionAsync(ticket.Guid, "Created", GetCurrentUserId());

            return ticket;
        }

        /// <summary>
        /// Notify all observers that a ticket was created
        /// </summary>
        private async Task NotifyObserversCreatedAsync(Ticket ticket)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    await observer.OnTicketCreatedAsync(ticket);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Observer {ObserverType} failed on ticket creation", observer.GetType().Name);
                    // Continue with other observers
                }
            }
        }

        /// <summary>
        /// Get detailed ticket information with GERDA insights
        /// </summary>
        public async Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketGuid)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: true);

            if (ticket == null)
            {
                return null;
            }

            var project = await _projectRepository.GetAllAsync();
            var ticketProject = project.FirstOrDefault(p => p.Tasks.Any(t => t.Guid == ticketGuid));

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
                ProjectGuid = ticketProject?.Guid,
                ProjectName = ticketProject?.Name,
                
                SubTickets = ticket.SubTickets.Select(st => new SubTicketInfo
                {
                    Guid = st.Guid,
                    Description = st.Description,
                    TicketStatus = st.TicketStatus
                }).ToList(),
                
                // GERDA AI Insights
                EstimatedEffortPoints = ticket.EstimatedEffortPoints,
                PriorityScore = ticket.PriorityScore,
                GerdaTags = ticket.GerdaTags,
                
                // Review Status
                ReviewStatus = ticket.ReviewStatus,
                QualityReviews = ticket.QualityReviews
            };

            return viewModel;
        }

        /// <summary>
        /// Assign a ticket to an agent
        /// Notifies observers after assignment
        /// </summary>
        public async Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: false);
            
            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketGuid} not found for assignment", ticketGuid);
                return false;
            }

            var agent = await _userRepository.GetEmployeeByIdAsync(agentId);
            
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
            await NotifyObserversAssignedAsync(ticket, agent);
            
            // Send notification to agent
            await _notificationService.NotifyUserAsync(
                agentId, 
                $"You have been assigned to ticket #{ticket.Guid.ToString().Substring(0, 8)}", 
                $"/Ticket/Detail/{ticket.Guid}", 
                "Info"
            );
            
            // Audit Log
            await _auditService.LogActionAsync(ticket.Guid, "Assigned", GetCurrentUserId(), "Responsible", null, agent.Name);

            return true;
        }
        
        /// <summary>
        /// Notify all observers that a ticket was assigned
        /// </summary>
        private async Task NotifyObserversAssignedAsync(Ticket ticket, Employee assignee)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    await observer.OnTicketAssignedAsync(ticket, assignee);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Observer {ObserverType} failed on ticket assignment", observer.GetType().Name);
                }
            }
        }

        /// <summary>
        /// Get employee by ID for GERDA recommendation details
        /// </summary>
        public async Task<Employee?> GetEmployeeByIdAsync(string agentId)
        {
            return await _userRepository.GetEmployeeByIdAsync(agentId);
        }

        /// <summary>
        /// Calculate current workload for an employee (sum of EstimatedEffortPoints for assigned/in-progress tickets)
        /// </summary>
        public async Task<int> GetEmployeeCurrentWorkloadAsync(string agentId)
        {
            var tickets = await _ticketRepository.GetByResponsibleIdAsync(agentId);
            
            var activeTickets = tickets.Where(t => 
                t.TicketStatus == Status.Assigned || 
                t.TicketStatus == Status.InProgress);
            
            return activeTickets.Sum(t => t.EstimatedEffortPoints);
        }

        /// <summary>
        /// Get ticket for editing with relations
        /// </summary>
        public async Task<Ticket?> GetTicketForEditAsync(Guid ticketGuid)
        {
            return await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: true);
        }

        /// <summary>
        /// Get all users (not just employees) for edit form dropdown
        /// </summary>
        public async Task<List<SelectListItem>> GetAllUsersSelectListAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();
            
            return users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FirstName} {u.LastName}"
            }).ToList();
        }

        /// <summary>
        /// Update an existing ticket
        /// </summary>
        public async Task<bool> UpdateTicketAsync(Ticket ticket)
        {
            try
            {
                await _ticketRepository.UpdateAsync(ticket);
                
                // Notify observers
                await NotifyObserversUpdatedAsync(ticket);
                
                // Notify responsible if status changed to Completed or Rejected
                if (ticket.ResponsibleId != null && (ticket.TicketStatus == Status.Completed || ticket.TicketStatus == Status.Rejected))
                {
                    await _notificationService.NotifyUserAsync(
                        ticket.ResponsibleId, 
                        $"Ticket #{ticket.Guid.ToString().Substring(0, 8)} status changed to {ticket.TicketStatus}", 
                        $"/Ticket/Detail/{ticket.Guid}", 
                        "Info"
                    );
                }
                
                // Audit Log
                await _auditService.LogActionAsync(ticket.Guid, "Updated", GetCurrentUserId());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update ticket {TicketGuid}", ticket.Guid);
                return false;
            }
        }

        /// <summary>
        /// Notify all observers that a ticket was updated
        /// </summary>
        private async Task NotifyObserversUpdatedAsync(Ticket ticket)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    await observer.OnTicketUpdatedAsync(ticket);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Observer {ObserverType} failed on ticket update", observer.GetType().Name);
                }
            }
        }

        /// <summary>
        /// Assign a ticket to an agent and/or project (manager functionality)
        /// </summary>
        public async Task<bool> AssignTicketWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: false);
            
            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketGuid} not found for assignment", ticketGuid);
                return false;
            }

            Employee? agent = null;
            
            if (!string.IsNullOrEmpty(agentId))
            {
                agent = await _userRepository.GetEmployeeByIdAsync(agentId);
                
                if (agent == null)
                {
                    _logger.LogWarning("Agent {AgentId} not found", agentId);
                    return false;
                }

                ticket.ResponsibleId = agentId;
                ticket.TicketStatus = Status.Assigned;
            }

            if (projectGuid.HasValue)
            {
                ticket.ProjectGuid = projectGuid.Value;
            }

            await _ticketRepository.UpdateAsync(ticket);

            _logger.LogInformation("Ticket {TicketGuid} assigned to agent {AgentId} and project {ProjectGuid}", 
                ticketGuid, agentId, projectGuid);
            
            // Notify observers if agent assigned
            if (agent != null)
            {
                await NotifyObserversAssignedAsync(ticket, agent);
            }
            else
            {
                // Just project assignment, notify update
                await NotifyObserversUpdatedAsync(ticket);
            }
            
            return true;
        }

        /// <summary>
        /// Batch assign tickets using GERDA recommendations or manual assignment
        /// Addresses remaining database coupling in ManagerController
        /// </summary>
        public async Task<BatchAssignResult> BatchAssignTicketsAsync(
            BatchAssignRequest request, 
            Func<Guid, Task<string?>> getRecommendedAgent)
        {
            var result = new BatchAssignResult();

            foreach (var ticketGuid in request.TicketGuids)
            {
                try
                {
                    var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: true);

                    if (ticket == null)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Ticket {ticketGuid} not found");
                        continue;
                    }

                    string? assignedAgentId = null;
                    Guid? assignedProjectGuid = null;

                    // Determine assignment strategy
                    if (request.UseGerdaRecommendations)
                    {
                        // Use GERDA to recommend agent
                        assignedAgentId = await getRecommendedAgent(ticketGuid);

                        // Use customer-based project recommendation
                        if (ticket.ProjectGuid == null && ticket.CustomerId != null)
                        {
                            var recommendedProject = await _projectRepository.GetRecommendedProjectForCustomerAsync(ticket.CustomerId);
                            assignedProjectGuid = recommendedProject?.Guid;
                        }
                    }
                    else
                    {
                        // Use forced assignments
                        assignedAgentId = request.ForceAgentId;
                        assignedProjectGuid = request.ForceProjectGuid;
                    }

                    // Apply assignments
                    Employee? assignedAgent = null;
                    if (!string.IsNullOrEmpty(assignedAgentId))
                    {
                        assignedAgent = await _userRepository.GetEmployeeByIdAsync(assignedAgentId);
                        if (assignedAgent != null)
                        {
                            ticket.ResponsibleId = assignedAgentId;
                            ticket.TicketStatus = Status.Assigned;
                            
                            if (request.UseGerdaRecommendations)
                            {
                                ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
                                    ? "AI-Dispatched"
                                    : $"{ticket.GerdaTags},AI-Dispatched";
                            }
                        }
                    }

                    if (assignedProjectGuid.HasValue)
                    {
                        ticket.ProjectGuid = assignedProjectGuid.Value;
                    }

                    await _ticketRepository.UpdateAsync(ticket);

                    // Get assigned names for result
                    var assignedProject = assignedProjectGuid.HasValue
                        ? await _projectRepository.GetByIdAsync(assignedProjectGuid.Value, includeRelations: false)
                        : null;

                    result.SuccessCount++;
                    result.Assignments.Add(new TicketAssignmentDetail
                    {
                        TicketGuid = ticketGuid,
                        AssignedAgentName = assignedAgent != null 
                            ? $"{assignedAgent.FirstName} {assignedAgent.LastName}" 
                            : null,
                        AssignedProjectName = assignedProject?.Name,
                        Success = true
                    });

                    // Notify observers if agent assigned
                    if (assignedAgent != null)
                    {
                        await NotifyObserversAssignedAsync(ticket, assignedAgent);
                    }
                    else if (assignedProjectGuid.HasValue)
                    {
                        await NotifyObserversUpdatedAsync(ticket);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning ticket {TicketGuid}", ticketGuid);
                    result.FailureCount++;
                    result.Errors.Add($"Error assigning ticket {ticketGuid}: {ex.Message}");
                    result.Assignments.Add(new TicketAssignmentDetail
                    {
                        TicketGuid = ticketGuid,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return result;
        }

        public async Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel)
        {
            var departmentId = await GetCurrentUserDepartmentIdAsync();
            return await _ticketRepository.SearchTicketsAsync(searchModel, departmentId);
        }

        public async Task BatchAssignToAgentAsync(List<Guid> ticketIds, string agentId)
        {
            foreach (var id in ticketIds)
            {
                await AssignTicketAsync(id, agentId);
            }
        }

        public async Task BatchUpdateStatusAsync(List<Guid> ticketIds, Status status)
        {
            foreach (var id in ticketIds)
            {
                var ticket = await _ticketRepository.GetByIdAsync(id, includeRelations: false);
                if (ticket != null)
                {
                    ticket.TicketStatus = status;
                    await UpdateTicketAsync(ticket);
                }
            }
        }
    }
}
