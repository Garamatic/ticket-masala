using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Domain.Data;
using TicketMasala.Web.Data;
using TicketMasala.Web.Data.Seeding;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Xunit;

namespace TicketMasala.Tests.UnitTests.Data;

public class DbSeederTests
{
    private readonly Mock<ILogger<DbSeeder>> _mockLogger;
    private readonly MasalaDbContext _context;

    public DbSeederTests()
    {
        _mockLogger = new Mock<ILogger<DbSeeder>>();

        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MasalaDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_ExecutesAllStrategies()
    {
        // Arrange
        var mockStrategy1 = new Mock<ISeedStrategy>();
        mockStrategy1.Setup(s => s.ShouldSeedAsync()).ReturnsAsync(true);

        var mockStrategy2 = new Mock<ISeedStrategy>();
        mockStrategy2.Setup(s => s.ShouldSeedAsync()).ReturnsAsync(true);

        var strategies = new List<ISeedStrategy> { mockStrategy1.Object, mockStrategy2.Object };

        var seeder = new DbSeeder(strategies, _context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync();

        // Assert
        mockStrategy1.Verify(s => s.SeedAsync(), Times.Once);
        mockStrategy2.Verify(s => s.SeedAsync(), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenStrategyFails_LogsErrorAndContinues()
    {
        // Arrange
        var mockStrategy1 = new Mock<ISeedStrategy>();
        mockStrategy1.Setup(s => s.ShouldSeedAsync()).ReturnsAsync(true);
        mockStrategy1.Setup(s => s.SeedAsync()).ThrowsAsync(new Exception("Strategy failed"));

        var mockStrategy2 = new Mock<ISeedStrategy>();
        mockStrategy2.Setup(s => s.ShouldSeedAsync()).ReturnsAsync(true);

        var strategies = new List<ISeedStrategy> { mockStrategy1.Object, mockStrategy2.Object };

        var seeder = new DbSeeder(strategies, _context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync();

        // Assert
        mockStrategy1.Verify(s => s.SeedAsync(), Times.Once);
        mockStrategy2.Verify(s => s.SeedAsync(), Times.Once); // Should still execute mockStrategy2

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error executing seed strategy")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
