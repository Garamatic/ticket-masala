using TicketMasala.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TicketMasala.Web.Abstractions;

namespace TicketMasala.Web.Data.Seeding;

/// <summary>
/// Seed strategy for creating knowledge base articles.
/// </summary>
public class KnowledgeBaseSeedStrategy : ISeedStrategy
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<KnowledgeBaseSeedStrategy> _logger;
    private readonly ISystemClock _clock;

    private readonly IWebHostEnvironment _environment;

    public KnowledgeBaseSeedStrategy(
        MasalaDbContext context,
        IWebHostEnvironment environment,
        ILogger<KnowledgeBaseSeedStrategy> logger,
        ISystemClock clock)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _clock = clock;
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

        var articles = new List<KnowledgeBaseArticle>();
        var config = await LoadSeedConfigurationAsync();
        var now = _clock.UtcNow;

        if (config?.KnowledgeBaseArticles?.Count > 0)
        {
             _logger.LogInformation("Loading {Count} KB articles from configuration", config.KnowledgeBaseArticles.Count);
            foreach (var dto in config.KnowledgeBaseArticles)
            {
                articles.Add(new KnowledgeBaseArticle
                {
                    Title = dto.Title,
                    Content = dto.Content,
                    Tags = dto.Tags,
                    CreatedAt = now,
                    UpdatedAt = now,
                    AuthorId = null // System article
                });
            }
        }
        else
        {
             _logger.LogInformation("No KB config found, using defaults");
             articles = CreateDefaultArticles();
        }

        foreach (var article in articles)
        {
            _context.KnowledgeBaseArticles.Add(article);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} knowledge base articles", articles.Count);
    }

    private async Task<SeedConfig?> LoadSeedConfigurationAsync()
    {
        var seedFilePath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigFilePath(
            _environment.ContentRootPath,
            "seed_data.json");

        if (!File.Exists(seedFilePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(seedFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<SeedConfig>(json, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing seed data JSON from {Path}", seedFilePath);
            return null;
        }
    }

    private List<KnowledgeBaseArticle> CreateDefaultArticles()
    {
        var now = _clock.UtcNow;
        return new List<KnowledgeBaseArticle>
        {
            new KnowledgeBaseArticle
            {
                Title = "Getting Started with Ticket Masala",
                Content = "Welcome to Ticket Masala! This guide will help you get started...",
                Tags = "getting-started,tutorial",
                CreatedAt = now,
                UpdatedAt = now
            },
            new KnowledgeBaseArticle
            {
                Title = "How to Create a Ticket",
                Content = "To create a new ticket, navigate to the Tickets page and click 'New Ticket'...",
                Tags = "tickets,how-to",
                CreatedAt = now,
                UpdatedAt = now
            },
            new KnowledgeBaseArticle
            {
                Title = "Understanding GERDA AI",
                Content = "GERDA (GovTech Extended Resource Dispatch & Anticipation) is our AI system...",
                Tags = "gerda,ai,automation",
                IsVerified = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new KnowledgeBaseArticle
            {
                Title = "VPN Connection Troubleshooting",
                Content = "If you are having trouble connecting to the VPN, try restarting your client and checking your internet connection. Ensure you are using the correct credentials.",
                Tags = "vpn,connectivity,remote",
                IsVerified = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new KnowledgeBaseArticle
            {
                Title = "Password Reset Guide",
                Content = "You can reset your password by clicking the 'Forgot Password' link on the login page. An email will be sent to your registered address with further instructions.",
                Tags = "password,account,security",
                IsVerified = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };
    }
}
