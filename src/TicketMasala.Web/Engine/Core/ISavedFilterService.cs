using TicketMasala.Domain.Entities;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Engine.Core;

public interface ISavedFilterService
{
    Task<List<SavedFilter>> GetFiltersForUserAsync(string userId);
    Task<SavedFilter?> GetFilterAsync(Guid id);
    Task<SavedFilter> SaveFilterAsync(string userId, string name, TicketSearchViewModel searchModel);
    Task DeleteFilterAsync(Guid id, string userId);
}
