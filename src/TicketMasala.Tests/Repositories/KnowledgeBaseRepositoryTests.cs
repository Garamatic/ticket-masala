using Xunit;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Data;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace TicketMasala.Tests.Repositories;

public class KnowledgeBaseRepositoryTests
{
    private readonly DbContextOptions<MasalaDbContext> _dbOptions;

    public KnowledgeBaseRepositoryTests()
    {
        _dbOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: "TestKnowledgeBaseDb_" + Guid.NewGuid())
            .Options;
    }

    [Fact]
    public async Task SearchAsync_SortsByMasalaRank()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var repository = new EfCoreKnowledgeBaseRepository(context);

        // MasalaRank = (UsageCount * 10) + (IsVerified * 50)
        
        var lowScore = new KnowledgeBaseArticle 
        { 
            Id = Guid.NewGuid(), 
            Title = "Low Score", 
            Content = "Content",
            UsageCount = 0,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        }; // Score 0

        var mediumScore = new KnowledgeBaseArticle 
        { 
            Id = Guid.NewGuid(), 
            Title = "Medium Score", 
            Content = "Content",
            UsageCount = 6,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        }; // Score 60

        var highScore = new KnowledgeBaseArticle 
        { 
            Id = Guid.NewGuid(), 
            Title = "High Score", 
            Content = "Content",
            UsageCount = 2,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        }; // Score 20 + 50 = 70

        var highestScore = new KnowledgeBaseArticle 
        { 
            Id = Guid.NewGuid(), 
            Title = "Highest Score", 
            Content = "Content",
            UsageCount = 10,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        }; // Score 100 + 50 = 150

        context.KnowledgeBaseArticles.AddRange(lowScore, mediumScore, highScore, highestScore);
        await context.SaveChangesAsync();

        // Act
        var results = await repository.SearchAsync("Content");

        // Assert
        var list = results.ToList();
        Assert.Equal(4, list.Count);
        Assert.Equal(highestScore.Id, list[0].Id); // 150
        Assert.Equal(highScore.Id, list[1].Id);    // 70
        Assert.Equal(mediumScore.Id, list[2].Id);  // 60
        Assert.Equal(lowScore.Id, list[3].Id);     // 0
    }

    [Fact]
    public async Task IncrementUsageCountAsync_IncreasesCount()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var repository = new EfCoreKnowledgeBaseRepository(context);

        var article = new KnowledgeBaseArticle 
        { 
            Id = Guid.NewGuid(), 
            Title = "Test", 
            Content = "Test",
            UsageCount = 0
        };
        context.KnowledgeBaseArticles.Add(article);
        await context.SaveChangesAsync();

        // Act
        await repository.IncrementUsageCountAsync(article.Id);

        // Assert
        var updated = await context.KnowledgeBaseArticles.FindAsync(article.Id);
        Assert.Equal(1, updated.UsageCount);
    }
}
