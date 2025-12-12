using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Data;
using TicketMasala.Web.Models;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Engine.Core;

public class SavedFilterService : ISavedFilterService
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<SavedFilterService> _logger;

    public SavedFilterService(MasalaDbContext context, ILogger<SavedFilterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SavedFilter>> GetFiltersForUserAsync(string userId)
    {
        return await _context.SavedFilters
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<SavedFilter?> GetFilterAsync(Guid id)
    {
        return await _context.SavedFilters.FindAsync(id);
    }

    public async Task<SavedFilter> SaveFilterAsync(string userId, string name, TicketSearchViewModel searchModel)
    {
        var filter = new SavedFilter
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            SearchTerm = searchModel.SearchTerm,
            Status = searchModel.Status,
            TicketType = searchModel.TicketType,
            ProjectId = searchModel.ProjectId,
            AssignedToId = searchModel.AssignedToId,
            CustomerId = searchModel.CustomerId,
            IsOverdue = searchModel.IsOverdue,
            IsDueSoon = searchModel.IsDueSoon
        };

        _context.SavedFilters.Add(filter);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved filter {FilterName} for user {UserId}", name, userId);
        return filter;
    }

    public async Task DeleteFilterAsync(Guid id, string userId)
    {
        var filter = await _context.SavedFilters.FindAsync(id);
        if (filter != null)
        {
            if (filter.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to delete filter {FilterId} owned by {OwnerId}",
                    userId, id, filter.UserId);
                throw new UnauthorizedAccessException("You can only delete your own filters.");
            }

            _context.SavedFilters.Remove(filter);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted filter {FilterId}", id);
        }
    }
}
