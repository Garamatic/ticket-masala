using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Pipeline.Stages;

/// <summary>
/// GERDA Stage: Knowledge (K).
/// Suggests relevant knowledge base articles for a ticket.
/// </summary>
public class KnowledgeStage : IGerdaStage
{
    private readonly IKnowledgeService? _knowledgeService;
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<KnowledgeStage> _logger;

    public KnowledgeStage(
        IKnowledgeService? knowledgeService,
        ITicketRepository ticketRepository,
        ILogger<KnowledgeStage> logger)
    {
        _knowledgeService = knowledgeService;
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public string StageName => "Knowledge";
    public bool IsEnabled => _knowledgeService != null;

    public async Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context)
    {
        if (_knowledgeService == null)
            return;

        var ticket = await _ticketRepository.GetByIdAsync(ticketGuid);
        if (ticket == null)
        {
            _logger.LogWarning("GERDA-K: Ticket {TicketGuid} not found", ticketGuid);
            return;
        }

        var suggestions = await _knowledgeService.GetSuggestedArticlesAsync(ticket);
        context.SuggestedArticles = suggestions.Select(s => s.Article.Id).ToList();

        _logger.LogInformation(
            "GERDA-K: Found {Count} suggested articles for ticket {TicketGuid}",
            suggestions.Count, ticketGuid);
    }
}
