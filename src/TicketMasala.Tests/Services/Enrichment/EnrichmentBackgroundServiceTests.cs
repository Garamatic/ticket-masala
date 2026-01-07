using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.Enrichment;
using Xunit;

namespace TicketMasala.Tests.Services.Enrichment;

public class EnrichmentBackgroundServiceTests
{
    private readonly Mock<ILogger<EnrichmentBackgroundService>> _mockLogger;
    private readonly Mock<IEnrichmentQueue> _mockQueue;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly DbContextOptions<MasalaDbContext> _dbOptions;

    public EnrichmentBackgroundServiceTests()
    {
        _mockLogger = new Mock<ILogger<EnrichmentBackgroundService>>();
        _mockQueue = new Mock<IEnrichmentQueue>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

        _dbOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesTicketWithSentiment()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var ticketId = Guid.NewGuid();
        // Use text that triggers "Critical" sentiment (e.g. "urgent", "fail")
        context.Tickets.Add(new Ticket { Guid = ticketId, Title = "Urgent Failure", Description = "System is down immediately", TicketStatus = Domain.Common.Status.Pending });
        await context.SaveChangesAsync();

        _mockServiceProvider.Setup(x => x.GetService(typeof(MasalaDbContext))).Returns(context);

        var workItem = new EnrichmentWorkItem { TicketId = ticketId, EnrichmentType = "Sentiment" };
        var cts = new CancellationTokenSource();

        int callCount = 0;
        _mockQueue.Setup(x => x.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => {
                callCount++;
                if (callCount == 1) return new ValueTask<EnrichmentWorkItem>(workItem);
                
                // Second call: wait until cancelled
                return new ValueTask<EnrichmentWorkItem>(Task.Delay(500, ct).ContinueWith(t => (EnrichmentWorkItem)null!));
            });

        var service = new EnrichmentBackgroundService(_mockLogger.Object, _mockQueue.Object, _mockScopeFactory.Object);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200); // Allow processing to happen
        
        // Trigger stop
        cts.Cancel();
        await service.StopAsync(default);

        // Assert
        var updatedTicket = await context.Tickets.FindAsync(ticketId);
        Assert.NotNull(updatedTicket);
        Assert.Contains("Sentiment:Critical", updatedTicket.GerdaTags);
        Assert.True(updatedTicket.PriorityScore > 0);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Enriching Ticket {ticketId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
