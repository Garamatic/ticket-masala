using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Repositories;

public class EfCoreKnowledgeBaseRepository : IKnowledgeBaseRepository
{
    private readonly MasalaDbContext _context;

    public EfCoreKnowledgeBaseRepository(MasalaDbContext context)
    {
        _context = context;
    }

    public async Task<KnowledgeBaseArticle?> GetByIdAsync(Guid id)
    {
        return await _context.KnowledgeBaseArticles
            .Include(a => a.Author)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<KnowledgeBaseArticle>> GetAllAsync()
    {
        return await _context.KnowledgeBaseArticles
            .Include(a => a.Author)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<KnowledgeBaseArticle>> SearchAsync(string searchTerm)
    {
        var query = _context.KnowledgeBaseArticles
            .Include(a => a.Author)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(term) ||
                a.Tags.ToLower().Contains(term) ||
                a.Content.ToLower().Contains(term));
        }

        // MasalaRank Implementation:
        // Score = (UsageCount * 10) + (IsVerified * 50)
        return await query
            .OrderByDescending(a => (a.UsageCount * 10) + (a.IsVerified ? 50 : 0))
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<KnowledgeBaseArticle> AddAsync(KnowledgeBaseArticle article)
    {
        _context.KnowledgeBaseArticles.Add(article);
        await _context.SaveChangesAsync();
        return article;
    }

    public async Task UpdateAsync(KnowledgeBaseArticle article)
    {
        _context.KnowledgeBaseArticles.Update(article);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var article = await _context.KnowledgeBaseArticles.FindAsync(id);
        if (article != null)
        {
            _context.KnowledgeBaseArticles.Remove(article);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.KnowledgeBaseArticles.AnyAsync(e => e.Id == id);
    }

    public async Task IncrementUsageCountAsync(Guid id)
    {
        var article = await _context.KnowledgeBaseArticles.FindAsync(id);
        if (article != null)
        {
            article.UsageCount++;
            await _context.SaveChangesAsync();
        }
    }
}
