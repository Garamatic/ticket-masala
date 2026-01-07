using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Repositories;

public interface IKnowledgeBaseRepository
{
    Task<KnowledgeBaseArticle?> GetByIdAsync(Guid id);
    Task<IEnumerable<KnowledgeBaseArticle>> GetAllAsync();
    Task<IEnumerable<KnowledgeBaseArticle>> SearchAsync(string searchTerm);
    Task<KnowledgeBaseArticle> AddAsync(KnowledgeBaseArticle article);
    Task UpdateAsync(KnowledgeBaseArticle article);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task IncrementUsageCountAsync(Guid id);
}
