using TicketMasala.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Data.Seeding;

/// <summary>
/// Seed strategy for creating knowledge base articles.
/// </summary>
public class KnowledgeBaseSeedStrategy : ISeedStrategy
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<KnowledgeBaseSeedStrategy> _logger;

    public KnowledgeBaseSeedStrategy(
        MasalaDbContext context,
        ILogger<KnowledgeBaseSeedStrategy> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ShouldSeedAsync()
    {
        // Seed if no KB articles exist
        var count = await _context.KnowledgeBaseArticles.CountAsync();
        return count == 0;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Seeding knowledge base articles...");

        var articles = CreateDefaultArticles();

        foreach (var article in articles)
        {
            _context.KnowledgeBaseArticles.Add(article);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} knowledge base articles", articles.Count);
    }

    private List<KnowledgeBaseArticle> CreateDefaultArticles()
    {
        return new List<KnowledgeBaseArticle>
        {
            new KnowledgeBaseArticle
            {
                Title = "Getting Started with Ticket Masala",
                Content = "Welcome to Ticket Masala! This guide will help you get started...",
                Tags = "getting-started,tutorial"
            },
            new KnowledgeBaseArticle
            {
                Title = "How to Create a Ticket",
                Content = "To create a new ticket, navigate to the Tickets page and click 'New Ticket'...",
                Tags = "tickets,how-to"
            },
            new KnowledgeBaseArticle
            {
                Title = "Understanding GERDA AI",
                Content = "GERDA (GovTech Extended Resource Dispatch & Anticipation) is our AI system...",
                Tags = "gerda,ai,automation"
            }
        };
    }
}
