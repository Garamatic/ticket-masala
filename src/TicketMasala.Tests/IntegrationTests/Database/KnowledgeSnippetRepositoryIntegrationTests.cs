using Xunit;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Repositories;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace TicketMasala.Tests.IntegrationTests.Database;

public class KnowledgeSnippetRepositoryIntegrationTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<MasalaDbContext> _contextOptions;

    public KnowledgeSnippetRepositoryIntegrationTests()
    {
        // Create and open a connection. This will hold the in-memory database alive.
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create the schema
        using var context = new MasalaDbContext(_contextOptions);
        
        // EnsureCreated creates tables for DbSets.
        context.Database.EnsureCreated();
        
        // Manually create FTS5 virtual table and triggers since EnsureCreated doesn't run migrations
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE VIRTUAL TABLE IF NOT EXISTS KnowledgeBaseSnippets_Search USING fts5(
                    Content,
                    Tags,
                    content='KnowledgeBaseSnippets',
                    content_rowid='rowid'
                );
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE TRIGGER IF NOT EXISTS KnowledgeBaseSnippets_AI AFTER INSERT ON KnowledgeBaseSnippets BEGIN
                    INSERT INTO KnowledgeBaseSnippets_Search(rowid, Content, Tags) 
                    VALUES (new.rowid, new.Content, new.Tags);
                END;
            ");
            
            // Note: We skip Delete/Update triggers for this specific test setup for brevity, 
            // but in real migration they exist.
        }
        catch (Exception ex)
        {
            // If FTS5 is not supported, tests might fail or we should skip.
            // But Microsoft.Data.Sqlite usually supports it.
            Console.WriteLine($"Warning: Failed to create FTS5 table: {ex.Message}");
        }
    }

    [Fact]
    public async Task SearchAsync_UsesFts5_AndReturnsResults()
    {
        using var context = new MasalaDbContext(_contextOptions);
        var repository = new EfCoreKnowledgeSnippetRepository(context);

        var snippet1 = new KnowledgeBaseSnippet { Content = "This is a test snippet about apples", Tags = "#fruit" };
        var snippet2 = new KnowledgeBaseSnippet { Content = "This is a test snippet about oranges", Tags = "#fruit" };
        var snippet3 = new KnowledgeBaseSnippet { Content = "This is a test snippet about cars", Tags = "#vehicle" };

        context.KnowledgeBaseSnippets.AddRange(snippet1, snippet2, snippet3);
        await context.SaveChangesAsync();

        // Search for "apples"
        var results = await repository.SearchAsync("apples");

        Assert.Single(results);
        Assert.Equal(snippet1.Id, results.First().Id);
        
        // Search for "fruit"
        var fruitResults = await repository.SearchAsync("fruit");
        Assert.Equal(2, fruitResults.Count());
    }

    [Fact]
    public async Task IncrementUsageCountAsync_IncrementsCount()
    {
        using var context = new MasalaDbContext(_contextOptions);
        var repository = new EfCoreKnowledgeSnippetRepository(context);

        var snippet = new KnowledgeBaseSnippet { Content = "Popular snippet", UsageCount = 10 };
        context.KnowledgeBaseSnippets.Add(snippet);
        await context.SaveChangesAsync();

        await repository.IncrementUsageCountAsync(snippet.Id);

        var updatedSnippet = await context.KnowledgeBaseSnippets.FindAsync(snippet.Id);
        Assert.Equal(11, updatedSnippet.UsageCount);
    }

    public void Dispose() => _connection.Dispose();
}
