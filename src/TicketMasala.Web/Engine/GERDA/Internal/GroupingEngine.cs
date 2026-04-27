using TicketMasala.Web.Engine.GERDA.Grouping;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Internal engine implementation for Grouping stage.
/// Wraps existing IGroupingService logic, hides it from callers.
/// </summary>
internal sealed class GroupingEngine : IGroupingEngine
{
    private readonly IGroupingService _groupingService;
    private readonly GerdaConfig _config;

    public GroupingEngine(
        IGroupingService groupingService,
        GerdaConfig config)
    {
        _groupingService = groupingService;
        _config = config;
    }

    public bool IsEnabled => _config.GerdaAI.SpamDetection.IsEnabled;

    public Task<Guid?> CheckAndGroupAsync(Guid ticketGuid)
    {
        return _groupingService.CheckAndGroupTicketAsync(ticketGuid);
    }
}

/// <summary>
/// No-op implementation when Grouping is disabled.
/// </summary>
internal sealed class NoOpGroupingEngine : IGroupingEngine
{
    public bool IsEnabled => false;

    public Task<Guid?> CheckAndGroupAsync(Guid ticketGuid)
    {
        return Task.FromResult<Guid?>(null);
    }
}
