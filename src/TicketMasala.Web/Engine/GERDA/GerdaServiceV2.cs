using System.Linq;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.GERDA.Anticipation;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Pipeline;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Refactored GERDA orchestrator using Pipeline pattern.
/// Replaces the previous god-object implementation with cleaner separation of concerns.
/// GERDA = GovTech Extended Resource Dispatch &amp; Anticipation
/// </summary>
public class GerdaServiceV2 : IGerdaService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IGerdaPipeline _pipeline;
    private readonly IAnticipationService? _anticipationService;
    private readonly GerdaConfig _config;
    private readonly ILogger<GerdaServiceV2> _logger;

    public GerdaServiceV2(
        ITicketRepository ticketRepository,
        IGerdaPipeline pipeline,
        GerdaConfig config,
        ILogger<GerdaServiceV2> logger,
        IAnticipationService? anticipationService = null)
    {
        _ticketRepository = ticketRepository;
        _pipeline = pipeline;
        _config = config;
        _logger = logger;
        _anticipationService = anticipationService;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled;

    public async Task ProcessTicketAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("GERDA is disabled, skipping ticket processing");
            return;
        }

        _logger.LogInformation("GERDA: Processing ticket {TicketGuid}", ticketGuid);

        try
        {
            // Execute pipeline - all stages run in sequence
            var result = await _pipeline.ExecuteAsync(ticketGuid);
            var context = result.Context;

            if (result.HasFailures)
            {
                _logger.LogWarning(
                    "GERDA: Pipeline completed with some failures for ticket {TicketGuid}. Failed stages: {FailedStages}",
                    ticketGuid,
                    string.Join(", ", result.GetAllErrors().Select(e => e.StageName)));
            }
            else
            {
                _logger.LogInformation(
                    "GERDA: Successfully processed ticket {TicketGuid}. Results: Parent={ParentGuid}, Effort={Effort}, Priority={Priority}, Agent={AgentId}",
                    ticketGuid,
                    context.ParentTicketGuid,
                    context.EffortPoints,
                    context.PriorityScore,
                    context.RecommendedAgentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GERDA: Error processing ticket {TicketGuid}", ticketGuid);
            throw;
        }
    }

    public async Task ProcessAllOpenTicketsAsync()
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("GERDA is disabled, skipping batch processing");
            return;
        }

        _logger.LogInformation("GERDA: Starting batch processing of all open tickets");

        var allTickets = await _ticketRepository.GetAllAsync(null);
        var openTicketGuids = allTickets
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .Select(t => t.Guid)
            .ToList();

        _logger.LogInformation("GERDA: Found {Count} open tickets to process", openTicketGuids.Count);

        foreach (var ticketGuid in openTicketGuids)
        {
            try
            {
                await ProcessTicketAsync(ticketGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GERDA: Failed to process ticket {TicketGuid}, continuing with next", ticketGuid);
            }
        }

        // A - Anticipation: Check capacity forecast (runs after all tickets processed)
        if (_anticipationService != null && _anticipationService.IsEnabled)
        {
            try
            {
                var risk = await _anticipationService.CheckCapacityRiskAsync();
                if (risk != null)
                {
                    _logger.LogWarning(
                        "GERDA-A: Capacity risk detected! {Message} (Risk: {Percentage}%)",
                        risk.AlertMessage, risk.RiskPercentage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GERDA-A: Failed to check capacity risk");
            }
        }

        _logger.LogInformation("GERDA: Completed batch processing");
    }
}
