using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Internal engine implementation for Knowledge stage (K).
/// </summary>
internal sealed class KnowledgeEngine : IKnowledgeEngine
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly GerdaConfig _config;

    public KnowledgeEngine(
        IKnowledgeService knowledgeService,
        GerdaConfig config)
    {
        _knowledgeService = knowledgeService;
        _config = config;
    }

    public bool IsEnabled => _config.GerdaAI.Knowledge.IsEnabled;

    public async Task<IEnumerable<Guid>> SuggestArticlesAsync(Ticket ticket)
    {
        var suggestions = await _knowledgeService.GetSuggestedArticlesAsync(ticket);
        return suggestions.Select(a => a.Article.Id);
    }
}

/// <summary>
/// No-op implementation when Knowledge is disabled.
/// </summary>
internal sealed class NoOpKnowledgeEngine : IKnowledgeEngine
{
    public bool IsEnabled => false;

    public Task<IEnumerable<Guid>> SuggestArticlesAsync(Ticket ticket)
    {
        return Task.FromResult<IEnumerable<Guid>>(Array.Empty<Guid>());
    }
}
