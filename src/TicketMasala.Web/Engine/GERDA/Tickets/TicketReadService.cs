using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Repositories;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Utilities;
using TicketMasala.Web.ViewModels.GERDA;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

public interface ITicketReadService
{
    Task<List<SelectListItem>> GetCustomerSelectListAsync();
    Task<List<SelectListItem>> GetEmployeeSelectListAsync();
    Task<List<SelectListItem>> GetProjectSelectListAsync();
    Task<List<SelectListItem>> GetCustomerProjectSelectListAsync(string customerId);
    Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync();
    Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketGuid);
    Task<Employee?> GetEmployeeByIdAsync(string agentId);
    Task<int> GetEmployeeCurrentWorkloadAsync(string agentId);
    Task<List<SelectListItem>> GetAllUsersSelectListAsync();
    Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel);
    Task<DashboardStats> GetDashboardStatsAsync(string? userId, bool isCustomer);
    Task<List<TicketViewModel>> GetRecentActivityAsync(string? userId, int count);
    Task<Guid?> GetCurrentUserDepartmentIdAsync();
    Task<Ticket?> GetTicketForEditAsync(Guid ticketGuid);
}

public class TicketReadService : ITicketReadService
{
    private readonly MasalaDbContext _context;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TicketReadService> _logger;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly Domain.TicketReportingService _ticketReportingService;
    private readonly ISystemClock _clock;

    public TicketReadService(
        MasalaDbContext context,
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TicketReadService> logger,
        IDomainConfigurationService domainConfig,
        Domain.TicketReportingService ticketReportingService,
        ISystemClock clock)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _domainConfig = domainConfig;
        _ticketReportingService = ticketReportingService;
        _clock = clock;
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? null;
    }

    public async Task<Guid?> GetCurrentUserDepartmentIdAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;

        var user = await _context.Users.FindAsync(userId).ConfigureAwait(false);
        var employee = user as Employee;
        if (employee?.DepartmentId != null && Guid.TryParse(employee.DepartmentId, out var deptGuid))
        {
            return deptGuid;
        }
        return null;
    }

    public async Task<List<SelectListItem>> GetCustomerSelectListAsync()
    {
        var customers = await _userRepository.GetAllCustomersAsync().ConfigureAwait(false);
        return customers.Select(c => new SelectListItem
        {
            Value = c.Id,
            Text = c.FullName
        }).ToList();
    }

    public async Task<List<SelectListItem>> GetEmployeeSelectListAsync()
    {
        var employees = await _userRepository.GetAllEmployeesAsync().ConfigureAwait(false);
        return employees.Select(e => new SelectListItem
        {
            Value = e.Id,
            Text = e.FullName
        }).ToList();
    }

    public async Task<List<SelectListItem>> GetProjectSelectListAsync()
    {
        var projects = await _projectRepository.GetAllAsync().ConfigureAwait(false);
        return projects.Select(p => new SelectListItem
        {
            Value = p.Guid.ToString(),
            Text = p.Name
        }).ToList();
    }

    public async Task<List<SelectListItem>> GetCustomerProjectSelectListAsync(string customerId)
    {
        var projects = await _projectRepository.GetActiveProjectsAsync().ConfigureAwait(false);
        return projects
            .Where(p => p.CustomerId == customerId || p.Customers.Any(c => c.Id == customerId))
            .Select(p => new SelectListItem
            {
                Value = p.Guid.ToString(),
                Text = p.Name
            }).ToList();
    }

    public async Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync()
    {
        var departmentId = await GetCurrentUserDepartmentIdAsync().ConfigureAwait(false);
        var tickets = await _ticketRepository.GetAllAsync(departmentId).ConfigureAwait(false);

        return tickets.Select(t => new TicketViewModel
        {
            Guid = t.Guid,
            Description = t.Description,
            TicketStatus = t.TicketStatus,
            CreationDate = t.CreationDate,
            CompletionTarget = t.CompletionTarget,
            ResponsibleName = t.Responsible?.FullName ?? "Not Assigned",
            CustomerName = t.Customer?.FullName ?? "Unknown",
            GerdaTags = t.GerdaTags
        }).ToList();
    }

    public async Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketGuid)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: true).ConfigureAwait(false);

        if (ticket == null)
        {
            return null;
        }

        var documents = await _ticketRepository.GetDocumentsForTicketAsync(ticketGuid).ConfigureAwait(false);
        var comments = await _ticketRepository.GetCommentsForTicketAsync(ticketGuid).ConfigureAwait(false);
        var reviews = await _ticketRepository.GetQualityReviewsForTicketAsync(ticketGuid).ConfigureAwait(false);
        var logs = await _auditService.GetAuditLogForTicketAsync(ticketGuid).ConfigureAwait(false);

        // Get project efficiently using the ticket's ProjectGuid if available
        Project? ticketProject = null;
        if (ticket.ProjectGuid.HasValue)
        {
            ticketProject = await _projectRepository.GetByIdAsync(ticket.ProjectGuid.Value, includeRelations: false).ConfigureAwait(false);
        }

        var viewModel = new TicketDetailsViewModel
        {
            Guid = ticket.Guid,
            Description = ticket.Description,
            TicketStatus = ticket.TicketStatus,
            TicketType = ticket.TicketType,
            CreationDate = ticket.CreationDate,
            CompletionTarget = ticket.CompletionTarget,
            CompletionDate = ticket.CompletionDate,
            Comments = comments.ToList(),
            Attachments = documents.ToList(),
            QualityReviews = reviews.ToList(),
            AuditLogs = logs,

            ResponsibleName = ticket.Responsible?.FullName,
            ResponsibleId = ticket.Responsible?.Id,
            CustomerName = ticket.Customer?.FullName,
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

            EstimatedEffortPoints = ticket.EstimatedEffortPoints,
            PriorityScore = ticket.PriorityScore,
            GerdaTags = ticket.GerdaTags,
            AiSummary = ticket.AiSummary ?? string.Empty,

            ReviewStatus = ticket.ReviewStatus,

            DomainId = ticket.DomainId,
            WorkItemTypeCode = ticket.WorkItemTypeCode,
            CustomFieldsJson = ticket.CustomFieldsJson
        };

        if (viewModel.CompletionTarget.HasValue && viewModel.TicketStatus != Status.Completed)
        {
            var timeRemaining = viewModel.CompletionTarget.Value - _clock.UtcNow;
            viewModel.IsCompletionOverdue = timeRemaining.TotalSeconds < 0;
            viewModel.IsCompletionDueSoon = !viewModel.IsCompletionOverdue && timeRemaining.TotalHours < 24;
            viewModel.HoursUntilCompletionTarget = timeRemaining.TotalHours;
        }

        // Calculate SLA status using ISystemClock
        if (viewModel.CompletionTarget.HasValue)
        {
            viewModel.DaysUntilSla = viewModel.GetDaysUntilSla(_clock);
            viewModel.IsSlaBreached = viewModel.IsSlaBreachedNow(_clock);
            viewModel.SlaStatusLabel = viewModel.GetSlaStatusLabel(_clock);
        }

        return viewModel;
    }

    public async Task<Employee?> GetEmployeeByIdAsync(string agentId)
    {
        return await _userRepository.GetEmployeeByIdAsync(agentId).ConfigureAwait(false);
    }

    public async Task<int> GetEmployeeCurrentWorkloadAsync(string agentId)
    {
        return await _ticketReportingService.GetEmployeeCurrentWorkloadAsync(agentId).ConfigureAwait(false);
    }

    public async Task<List<SelectListItem>> GetAllUsersSelectListAsync()
    {
        var users = await _userRepository.GetAllUsersAsync().ConfigureAwait(false);

        return users.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = u.FullName
        }).ToList();
    }

    public async Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel)
    {
        var departmentId = await GetCurrentUserDepartmentIdAsync().ConfigureAwait(false);

        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userRepository.GetUserByIdAsync(userId).ConfigureAwait(false);
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Constants.RoleAdmin) ?? false;

            if (user != null && user is not Employee && !isAdmin)
            {
                searchModel.CustomerId = userId;
            }
        }

        var query = new TicketMasala.Domain.Repositories.TicketSearchQuery
        {
            SearchTerm = searchModel.SearchTerm,
            Status = searchModel.Status,
            TicketType = searchModel.TicketType,
            ResponsibleId = searchModel.ResponsibleId,
            ProjectId = searchModel.ProjectId,
            CustomerId = searchModel.CustomerId,
            DateFrom = searchModel.DateFrom,
            DateTo = searchModel.DateTo,
            DepartmentId = departmentId,
            Page = searchModel.Page,
            PageSize = searchModel.PageSize
        };

        var (results, totalItems) = await _ticketRepository.SearchAsync(query).ConfigureAwait(false);

        searchModel.Results = results.ToList();
        searchModel.TotalItems = totalItems;

        return searchModel;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(string? userId, bool isCustomer)
    {
        var stats = new DashboardStats();
        var now = _clock.UtcNow;
        var weekAgo = now.AddDays(-7);
        var todayStart = now.Date;

        try
        {
            IQueryable<Ticket> ticketQuery = _context.Tickets.AsNoTracking();
            IQueryable<Project> projectQuery = _context.Projects.AsNoTracking();

            if (isCustomer && !string.IsNullOrEmpty(userId))
            {
                ticketQuery = ticketQuery.Where(t => t.CustomerId == userId);
                projectQuery = projectQuery.Where(p => p.CustomerId == userId);
            }

            stats.ProjectCount = await projectQuery.CountAsync().ConfigureAwait(false);
            stats.NewProjectsThisWeek = await projectQuery
                .Where(p => p.CreationDate >= weekAgo)
                .CountAsync().ConfigureAwait(false);

            stats.ActiveTicketCount = await ticketQuery
                .Where(t => t.TicketStatus != Status.Completed)
                .CountAsync().ConfigureAwait(false);

            stats.PendingTaskCount = await ticketQuery
                .Where(t => t.TicketStatus == Status.Pending)
                .CountAsync().ConfigureAwait(false);

            stats.CompletedToday = await ticketQuery
                .Where(t => t.TicketStatus == Status.Completed && t.CompletionDate >= todayStart)
                .CountAsync().ConfigureAwait(false);

            var threeDaysFromNow = now.AddDays(3);
            stats.DueSoon = await ticketQuery
                .Where(t => t.CompletionTarget.HasValue &&
                           t.CompletionTarget.Value <= threeDaysFromNow &&
                           t.TicketStatus != Status.Completed)
                .CountAsync().ConfigureAwait(false);

            var totalTickets = await ticketQuery.CountAsync().ConfigureAwait(false);
            var completedTickets = await ticketQuery.Where(t => t.TicketStatus == Status.Completed).CountAsync().ConfigureAwait(false);
            stats.CompletionRate = totalTickets > 0 ? (int)((double)completedTickets / totalTickets * 100) : 0;

            stats.HighRiskCount = await ticketQuery
                .Where(t => t.GerdaTags != null && (t.GerdaTags.Contains("Risk:Critical") || t.GerdaTags.Contains("Risk:High") || t.GerdaTags.Contains("Compliance:Violation")))
                .CountAsync().ConfigureAwait(false);

            stats.SentimentWarningCount = await ticketQuery
                .Where(t => t.GerdaTags != null && t.GerdaTags.Contains("Sentiment:Negative"))
                .CountAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard stats for user {UserId}", userId);
            // Return empty stats on error rather than partial data
            return new DashboardStats();
        }

        return stats;
    }

    public async Task<List<TicketViewModel>> GetRecentActivityAsync(string? userId, int count)
    {
        IQueryable<Ticket> query = _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userRepository.GetUserByIdAsync(userId).ConfigureAwait(false);
            if (user is not Employee)
            {
                query = query.Where(t => t.CustomerId == userId);
            }
        }

        var tickets = await query
            .OrderByDescending(t => t.CreationDate)
            .Take(count)
            .ToListAsync().ConfigureAwait(false);

        return tickets.Select(t => new TicketViewModel
        {
            Guid = t.Guid,
            Description = t.Description,
            TicketStatus = t.TicketStatus,
            CreationDate = t.CreationDate,
            CompletionTarget = t.CompletionTarget,
            ResponsibleName = t.Responsible?.FullName ?? "Unassigned",
            CustomerName = t.Customer?.FullName ?? "Unknown",
            GerdaTags = t.GerdaTags
        }).ToList();
    }

    public async Task<Ticket?> GetTicketForEditAsync(Guid ticketGuid)
    {
        return await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: true).ConfigureAwait(false);
    }
}
