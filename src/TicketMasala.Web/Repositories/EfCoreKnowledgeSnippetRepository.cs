using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Repositories;

public class EfCoreKnowledgeSnippetRepository : IKnowledgeSnippetRepository
{
    private readonly MasalaDbContext _context;

    public EfCoreKnowledgeSnippetRepository(MasalaDbContext context)
    {
        _context = context;
    }

    public async Task<KnowledgeBaseSnippet?> GetByIdAsync(Guid id)
    {
        return await _context.KnowledgeBaseSnippets
            .Include(s => s.Author)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<KnowledgeBaseSnippet>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
             return await _context.KnowledgeBaseSnippets
                .Include(s => s.Author)
                .OrderByDescending(s => s.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        // FTS5 Query
        // We join the virtual table 'KnowledgeBaseSnippets_Search' with the real table using rowid.
        // This leverages the FTS5 index for fast full-text search.
        
        var sql = @"
            SELECT s.* 
            FROM KnowledgeBaseSnippets s
            JOIN KnowledgeBaseSnippets_Search fts ON s.rowid = fts.rowid
            WHERE fts.KnowledgeBaseSnippets_Search MATCH {0}
            ORDER BY fts.rank
        ";

        try 
        {
            return await _context.KnowledgeBaseSnippets
                .FromSqlRaw(sql, searchTerm)
                .Include(s => s.Author)
                .ToListAsync();
        }
        catch (Exception)
        {
            // Fallback to LIKE if FTS5 fails (e.g. invalid syntax or table missing in test env)
            return await _context.KnowledgeBaseSnippets
                .Include(s => s.Author)
                .Where(s => s.Content.Contains(searchTerm) || s.Tags.Contains(searchTerm))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
    }

    public async Task<KnowledgeBaseSnippet> AddAsync(KnowledgeBaseSnippet snippet)
    {
        _context.KnowledgeBaseSnippets.Add(snippet);
        await _context.SaveChangesAsync();
        return snippet;
    }

    public async Task IncrementUsageCountAsync(Guid id)
    {
        var snippet = await _context.KnowledgeBaseSnippets.FindAsync(id);
        if (snippet != null)
        {
            snippet.UsageCount++;
            await _context.SaveChangesAsync();
        }
    }
}
