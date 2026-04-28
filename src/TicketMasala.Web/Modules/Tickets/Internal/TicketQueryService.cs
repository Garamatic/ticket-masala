using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;

namespace TicketMasala.Web.Modules.Tickets.Internal;

internal interface ITicketQueryService
{
    Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations, CancellationToken ct);
    Task<TicketSearchResult> SearchAsync(TicketSearchQuery query, string? requestingUserId, CancellationToken ct);
}

internal class TicketQueryService : ITicketQueryService
{
    private readonly MasalaDbContext _context;
    private readonly ITicketAuthorizationService _auth;
    private readonly ISystemClock _clock;

    public TicketQueryService(
        MasalaDbContext context,
        ITicketAuthorizationService auth,
        ISystemClock clock)
    {
        _context = context;
        _auth = auth;
        _clock = clock;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations, CancellationToken ct)
    {
        var query = _context.Tickets.AsQueryable();
        if (includeRelations)
        {
            query = query
                .Include(t => t.Customer)
                .Include(t => t.Responsible)
                .Include(t => t.Project);
        }
        return await query.FirstOrDefaultAsync(t => t.Guid == id, ct);
    }

    public async Task<TicketSearchResult> SearchAsync(TicketSearchQuery query, string? requestingUserId, CancellationToken ct)
    {
        // Implementation encapsulates the complex query logic from TicketReadService
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

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            dbQuery = dbQuery.Where(t => t.Status == query.Status);
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

        var totalCount = await dbQuery.CountAsync(ct);

        var items = await dbQuery
            .OrderByDescending(t => t.CreationDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TicketSummaryDto(
                t.Guid,
                t.Title,
                t.Status,
                t.CreationDate,
                t.Responsible != null ? $"{t.Responsible.FirstName} {t.Responsible.LastName}" : null))
            .ToListAsync(ct);

        return new TicketSearchResult(items, totalCount, query.Page, query.PageSize);
    }
}
