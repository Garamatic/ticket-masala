using TicketMasala.Web.Engine.GERDA.Grouping;

namespace TicketMasala.Web.Engine.GERDA.Pipeline.Stages;

/// <summary>
/// GERDA Stage: Grouping (G).
/// Checks for duplicate/spam tickets and groups them under a parent.
/// </summary>
public class GroupingStage : IGerdaStage
{
    private readonly IGroupingService _groupingService;
    private readonly ILogger<GroupingStage> _logger;

    public GroupingStage(IGroupingService groupingService, ILogger<GroupingStage> logger)
    {
        _groupingService = groupingService;
        _logger = logger;
    }

    public string StageName => "Grouping";
    public bool IsEnabled => true; // Always enabled

    public async Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context)
    {
        var parentGuid = await _groupingService.CheckAndGroupTicketAsync(ticketGuid);
        
        if (parentGuid.HasValue)
        {
            context.ParentTicketGuid = parentGuid;
            _logger.LogInformation(
                "GERDA-G: Ticket {TicketGuid} grouped under parent {ParentGuid}",
                ticketGuid, parentGuid);
        }
    }
}
