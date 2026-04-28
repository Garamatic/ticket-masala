using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Repositories;
using TicketMasala.Web.Modules.Tickets;
using TicketMasala.Web.ViewModels.Tickets;

// Alias to resolve ambiguity between Module DTO and Domain DTO
using ModuleTicketSearchResult = TicketMasala.Web.Modules.Tickets.TicketSearchResult;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Canonical query service for tickets.
/// All ticket queries should go through this service to ensure consistent
/// filtering, authorization, and performance optimization.
/// </summary>
/// <remarks>
/// This is the single source of truth for ticket queries.
/// Replaces the duplicated query logic between TicketReadService and TicketQueryService.
/// </remarks>
public interface ITicketQueryService
{
    /// <summary>
    /// Gets a single ticket by ID.
    /// </summary>
    Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations, CancellationToken ct = default);

    /// <summary>
    /// Gets a ticket for editing with full details.
    /// </summary>
    Task<Ticket?> GetForEditAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Searches tickets using the domain query model.
    /// </summary>
    Task<ModuleTicketSearchResult> SearchAsync(TicketSearchQuery query, CancellationToken ct = default);

    /// <summary>
    /// Searches tickets for UI with view model mapping.
    /// </summary>
    Task<TicketSearchViewModel> SearchForUiAsync(TicketSearchViewModel searchModel, string? currentUserId, bool isAdmin, CancellationToken ct = default);

    /// <summary>
    /// Gets customer select list for dropdowns.
    /// </summary>
    Task<List<SelectListItem>> GetCustomerSelectListAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets employee select list for dropdowns.
    /// </summary>
    Task<List<SelectListItem>> GetEmployeeSelectListAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets project select list for dropdowns.
    /// </summary>
    Task<List<SelectListItem>> GetProjectSelectListAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all users select list for dropdowns.
    /// </summary>
    Task<List<SelectListItem>> GetAllUsersSelectListAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets current user's department ID if applicable.
    /// </summary>
    Task<Guid?> GetCurrentUserDepartmentIdAsync(string userId, CancellationToken ct = default);
}

/// <summary>
/// Implementation of the canonical ticket query service.
/// </summary>
internal class TicketQueryService : ITicketQueryService
{
    private readonly MasalaDbContext _context;
    private readonly IUserRepository _userRepository;

    public TicketQueryService(
        MasalaDbContext context,
        IUserRepository userRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations, CancellationToken ct = default)
    {
        var query = _context.Tickets.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(t => t.Customer)
                .Include(t => t.Responsible)
                .Include(t => t.Project)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.Author)
                .Include(t => t.SubTickets);
        }

        return await query.FirstOrDefaultAsync(t => t.Guid == id, ct).ConfigureAwait(false);
    }

    public async Task<Ticket?> GetForEditAsync(Guid id, CancellationToken ct = default)
    {
        // For editing, we need customer and responsible for authorization checks
        return await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .FirstOrDefaultAsync(t => t.Guid == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<TicketSearchResult> SearchAsync(TicketSearchQuery query, CancellationToken ct = default)
    {
        var dbQuery = BuildSearchQuery(query);

        var totalCount = await dbQuery.CountAsync(ct).ConfigureAwait(false);

        // Materialize ticket data with efficient projection
        var ticketData = await dbQuery
            .OrderByDescending(t => t.CreationDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new
            {
                t.Guid,
                t.Title,
                t.TicketStatus,
                t.CreationDate,
                t.ResponsibleId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Batch lookup employee names
        var responsibleIds = ticketData
            .Where(t => !string.IsNullOrEmpty(t.ResponsibleId))
            .Select(t => t.ResponsibleId!)
            .Distinct()
            .ToList();

        var employeeNames = responsibleIds.Count > 0
            ? await _context.Employees
                .AsNoTracking()
                .Where(e => responsibleIds.Contains(e.Id))
                .Select(e => new { e.Id, FullName = $"{e.FirstName} {e.LastName}" })
                .ToDictionaryAsync(e => e.Id, e => e.FullName, ct)
                .ConfigureAwait(false)
            : new Dictionary<string, string>();

        // Map to module DTOs
        var items = ticketData
            .Select(t => new TicketSummaryDto(
                t.Guid,
                t.Title,
                t.TicketStatus.ToString(),
                t.CreationDate,
                t.ResponsibleId != null && employeeNames.TryGetValue(t.ResponsibleId, out var name)
                    ? name
                    : null))
            .ToList();

        return new ModuleTicketSearchResult(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<TicketSearchViewModel> SearchForUiAsync(
        TicketSearchViewModel searchModel,
        string? currentUserId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        // Apply customer filter for non-admin, non-employee users
        if (!string.IsNullOrEmpty(currentUserId) && !isAdmin)
        {
            var user = await _userRepository.GetUserByIdAsync(currentUserId).ConfigureAwait(false);
            if (user is not Employee)
            {
                searchModel.CustomerId = currentUserId;
            }
        }

        // Get department filter if applicable
        Guid? departmentId = null;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            departmentId = await GetCurrentUserDepartmentIdAsync(currentUserId, ct).ConfigureAwait(false);
        }

        var query = new TicketSearchQuery
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

        // Execute search directly to populate domain DTOs that ViewModel expects
        var dbQuery = BuildSearchQuery(query);

        var totalCount = await dbQuery.CountAsync(ct).ConfigureAwait(false);

        var ticketData = await dbQuery
            .OrderByDescending(t => t.CreationDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new
            {
                t.Guid,
                t.Title,
                t.Description,
                t.TicketStatus,
                t.CreationDate,
                t.CompletionTarget,
                t.ResponsibleId,
                t.CustomerId,
                t.ProjectGuid,
                t.GerdaTags
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Batch lookup names
        var responsibleIds = ticketData.Where(t => !string.IsNullOrEmpty(t.ResponsibleId)).Select(t => t.ResponsibleId!).Distinct().ToList();
        var customerIds = ticketData.Where(t => !string.IsNullOrEmpty(t.CustomerId)).Select(t => t.CustomerId!).Distinct().ToList();

        var employeeNames = responsibleIds.Count > 0
            ? await _context.Employees.AsNoTracking().Where(e => responsibleIds.Contains(e.Id)).Select(e => new { e.Id, FullName = $"{e.FirstName} {e.LastName}" }).ToDictionaryAsync(e => e.Id, e => e.FullName, ct).ConfigureAwait(false)
            : new Dictionary<string, string>();

        var customerNames = customerIds.Count > 0
            ? await _context.Users.AsNoTracking().Where(u => customerIds.Contains(u.Id)).Select(u => new { u.Id, FullName = u.FullName }).ToDictionaryAsync(u => u.Id, u => u.FullName, ct).ConfigureAwait(false)
            : new Dictionary<string, string>();

        // Map to domain DTOs that ViewModel expects
        searchModel.Results = ticketData.Select(t => new TicketSearchResultDto
        {
            Guid = t.Guid,
            Title = t.Title ?? string.Empty,
            Description = t.Description,
            TicketStatus = t.TicketStatus,
            CreationDate = t.CreationDate,
            CompletionTarget = t.CompletionTarget,
            ResponsibleName = t.ResponsibleId != null && employeeNames.TryGetValue(t.ResponsibleId, out var empName) ? empName : null,
            CustomerName = t.CustomerId != null && customerNames.TryGetValue(t.CustomerId, out var custName) ? custName : null,
            ProjectGuid = t.ProjectGuid,
            GerdaTags = t.GerdaTags
        }).ToList();

        searchModel.TotalItems = totalCount;

        return searchModel;
    }

    public async Task<List<SelectListItem>> GetCustomerSelectListAsync(CancellationToken ct = default)
    {
        var customers = await _userRepository.GetAllCustomersAsync().ConfigureAwait(false);

        return customers
            .Select(c => new SelectListItem { Value = c.Id, Text = c.FullName })
            .ToList();
    }

    public async Task<List<SelectListItem>> GetEmployeeSelectListAsync(CancellationToken ct = default)
    {
        var employees = await _userRepository.GetAllEmployeesAsync().ConfigureAwait(false);

        return employees
            .Select(e => new SelectListItem { Value = e.Id, Text = e.FullName })
            .OrderBy(e => e.Text)
            .ToList();
    }

    public async Task<List<SelectListItem>> GetProjectSelectListAsync(CancellationToken ct = default)
    {
        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.ValidUntil == null)
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem { Value = p.Guid.ToString(), Text = p.Name })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return projects;
    }

    public async Task<List<SelectListItem>> GetAllUsersSelectListAsync(CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllUsersAsync().ConfigureAwait(false);

        return users
            .Select(u => new SelectListItem { Value = u.Id, Text = u.FullName })
            .ToList();
    }

    public async Task<Guid?> GetCurrentUserDepartmentIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct).ConfigureAwait(false);
        var employee = user as Employee;

        if (employee?.DepartmentId != null && Guid.TryParse(employee.DepartmentId, out var deptGuid))
        {
            return deptGuid;
        }

        return null;
    }

    private IQueryable<Ticket> BuildSearchQuery(TicketSearchQuery query)
    {
        var dbQuery = _context.Tickets
            .AsNoTracking()
            .Where(t => t.ValidUntil == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            dbQuery = dbQuery.Where(t =>
                t.Title.Contains(query.SearchTerm) ||
                t.Description.Contains(query.SearchTerm));
        }

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.TicketStatus == query.Status.Value);
        }

        if (query.TicketType.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.TicketType == query.TicketType.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ResponsibleId))
        {
            dbQuery = dbQuery.Where(t => t.ResponsibleId == query.ResponsibleId);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerId))
        {
            dbQuery = dbQuery.Where(t => t.CustomerId == query.CustomerId);
        }

        if (query.ProjectId.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.ProjectGuid == query.ProjectId.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            var deptIdString = query.DepartmentId.Value.ToString();
            dbQuery = dbQuery.Where(t =>
                t.ResponsibleId != null &&
                _context.Employees.Any(e => e.Id == t.ResponsibleId && e.DepartmentId == deptIdString));
        }

        if (query.DateFrom.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.CreationDate >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.CreationDate <= query.DateTo.Value);
        }

        return dbQuery;
    }
}
