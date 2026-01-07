using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Repositories;

public interface IKnowledgeSnippetRepository
{
    Task<KnowledgeBaseSnippet?> GetByIdAsync(Guid id);
    Task<IEnumerable<KnowledgeBaseSnippet>> SearchAsync(string searchTerm);
    Task<KnowledgeBaseSnippet> AddAsync(KnowledgeBaseSnippet snippet);
    Task IncrementUsageCountAsync(Guid id);
}
