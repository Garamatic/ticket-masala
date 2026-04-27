using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Engine.Core;

namespace TicketMasala.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private readonly MasalaDbContext _context;
    private readonly Mock<ISystemClock> _mockClock;
    private readonly Mock<ILogger<AuditService>> _mockLogger;
    private readonly AuditService _service;
    private readonly DateTime _fixedTime;

    public AuditServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MasalaDbContext(options);

        // Setup fixed time for deterministic tests
        _fixedTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        _mockClock = new Mock<ISystemClock>();
        _mockClock.Setup(c => c.UtcNow).Returns(_fixedTime);

        _mockLogger = new Mock<ILogger<AuditService>>();
        _service = new AuditService(_context, _mockClock.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region LogActionAsync Tests

    [Fact]
    public async Task LogActionAsync_WithValidData_ShouldCreateAuditEntry()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = "user-123";
        var action = "TicketCreated";
        var propertyName = "Status";
        var oldValue = "Draft";
        var newValue = "Open";

        // Act
        await _service.LogActionAsync(ticketId, action, userId, propertyName, oldValue, newValue);

        // Assert
        var entries = await _context.AuditLogs.ToListAsync();
        entries.Should().HaveCount(1);

        var entry = entries.First();
        entry.TicketId.Should().Be(ticketId);
        entry.Action.Should().Be(action);
        entry.UserId.Should().Be(userId);
        entry.PropertyName.Should().Be(propertyName);
        entry.OldValue.Should().Be(oldValue);
        entry.NewValue.Should().Be(newValue);
        entry.Timestamp.Should().Be(_fixedTime);
        entry.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task LogActionAsync_WithMinimalData_ShouldCreateAuditEntry()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var action = "TicketViewed";

        // Act
        await _service.LogActionAsync(ticketId, action, null, null, null, null);

        // Assert
        var entries = await _context.AuditLogs.ToListAsync();
        entries.Should().HaveCount(1);

        var entry = entries.First();
        entry.TicketId.Should().Be(ticketId);
        entry.Action.Should().Be(action);
        entry.UserId.Should().BeNull();
        entry.PropertyName.Should().BeNull();
        entry.OldValue.Should().BeNull();
        entry.NewValue.Should().BeNull();
    }

    [Fact]
    public async Task LogActionAsync_MultipleCalls_ShouldCreateMultipleEntries()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        await _service.LogActionAsync(ticketId, "Created", "user-1");
        await _service.LogActionAsync(ticketId, "Updated", "user-2", "Status", "Open", "InProgress");
        await _service.LogActionAsync(ticketId, "Assigned", "user-3", "Assignee", null, "user-3");

        // Assert
        var entries = await _context.AuditLogs
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        entries.Should().HaveCount(3);
        entries.Select(e => e.Action).Should().ContainInOrder("Created", "Updated", "Assigned");
    }

    [Fact]
    public async Task LogActionAsync_DifferentTickets_ShouldCreateSeparateEntries()
    {
        // Arrange
        var ticketId1 = Guid.NewGuid();
        var ticketId2 = Guid.NewGuid();

        // Act
        await _service.LogActionAsync(ticketId1, "Action1", "user-1");
        await _service.LogActionAsync(ticketId2, "Action2", "user-2");

        // Assert
        var entries = await _context.AuditLogs.ToListAsync();
        entries.Should().HaveCount(2);

        entries.Select(e => e.TicketId).Should().Contain(new[] { ticketId1, ticketId2 });
    }

    [Fact]
    public async Task LogActionAsync_WhenDbExceptionOccurs_ShouldNotThrowAndLogError()
    {
        // Arrange - Create a context that will fail on save
        var failingOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var failingContext = new MasalaDbContext(failingOptions);

        // Dispose to make it fail on subsequent operations
        failingContext.Dispose();

        var serviceWithFailingContext = new AuditService(failingContext, _mockClock.Object, _mockLogger.Object);

        // Act - Should not throw despite database being disposed
        var exception = await Record.ExceptionAsync(() =>
            serviceWithFailingContext.LogActionAsync(Guid.NewGuid(), "Action", "user-1"));

        // Assert
        exception.Should().BeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to log audit entry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetAuditLogForTicketAsync Tests

    [Fact]
    public async Task GetAuditLogForTicketAsync_WithNoEntries_ShouldReturnEmptyList()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var result = await _service.GetAuditLogForTicketAsync(ticketId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditLogForTicketAsync_WithEntries_ShouldReturnOrderedByTimestampDesc()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = "user-123";

        var time1 = _fixedTime.AddHours(-2);
        var time2 = _fixedTime.AddHours(-1);
        var time3 = _fixedTime;

        _context.AuditLogs.AddRange(
            new AuditLogEntry { Id = Guid.NewGuid(), TicketId = ticketId, Action = "Old", Timestamp = time1, UserId = userId },
            new AuditLogEntry { Id = Guid.NewGuid(), TicketId = ticketId, Action = "Middle", Timestamp = time2, UserId = userId },
            new AuditLogEntry { Id = Guid.NewGuid(), TicketId = ticketId, Action = "New", Timestamp = time3, UserId = userId }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAuditLogForTicketAsync(ticketId);

        // Assert
        result.Should().HaveCount(3);
        result.Select(e => e.Action).Should().ContainInOrder("New", "Middle", "Old");
    }

    [Fact]
    public async Task GetAuditLogForTicketAsync_WithOtherTickets_ShouldOnlyReturnSpecifiedTicket()
    {
        // Arrange
        var targetTicketId = Guid.NewGuid();
        var otherTicketId = Guid.NewGuid();
        var userId = "user-123";

        _context.AuditLogs.AddRange(
            new AuditLogEntry { Id = Guid.NewGuid(), TicketId = targetTicketId, Action = "TargetAction", Timestamp = _fixedTime, UserId = userId },
            new AuditLogEntry { Id = Guid.NewGuid(), TicketId = otherTicketId, Action = "OtherAction", Timestamp = _fixedTime, UserId = userId },
            new AuditLogEntry { Id = Guid.NewGuid(), TicketId = targetTicketId, Action = "TargetAction2", Timestamp = _fixedTime.AddMinutes(-5), UserId = userId }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAuditLogForTicketAsync(targetTicketId);

        // Assert
        result.Should().HaveCount(2);
        result.All(e => e.TicketId == targetTicketId).Should().BeTrue();
        result.Select(e => e.Action).Should().Contain("TargetAction", "TargetAction2");
        result.Select(e => e.Action).Should().NotContain("OtherAction");
    }

    [Fact]
    public async Task GetAuditLogForTicketAsync_ShouldIncludeUserNavigation()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        _context.Users.Add(user);
        _context.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Action = "Created",
            Timestamp = _fixedTime,
            UserId = userId
        });
        await _context.SaveChangesAsync();

        // Detach to ensure we're testing the Include
        _context.ChangeTracker.Clear();

        // Act
        var result = await _service.GetAuditLogForTicketAsync(ticketId);

        // Assert
        result.Should().HaveCount(1);
        result.First().User.Should().NotBeNull();
        result.First().User!.UserName.Should().Be("testuser");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task LogActionAndRetrieve_RoundTrip_ShouldPersistAndRetrieve()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = "user-123";

        // Act - Log multiple actions with distinct timestamps
        _mockClock.Setup(c => c.UtcNow).Returns(_fixedTime.AddMinutes(-10));
        await _service.LogActionAsync(ticketId, "Created", userId);

        _mockClock.Setup(c => c.UtcNow).Returns(_fixedTime.AddMinutes(-5));
        await _service.LogActionAsync(ticketId, "Updated", userId, "Status", "Open", "InProgress");

        _mockClock.Setup(c => c.UtcNow).Returns(_fixedTime);
        await _service.LogActionAsync(ticketId, "Closed", userId, "Status", "InProgress", "Closed");

        // Retrieve
        var logs = await _service.GetAuditLogForTicketAsync(ticketId);

        // Assert
        logs.Should().HaveCount(3);
        logs.Select(l => l.Action).Should().ContainInOrder("Closed", "Updated", "Created");

        // Verify the full history
        var closedEntry = logs.First(l => l.Action == "Closed");
        closedEntry.PropertyName.Should().Be("Status");
        closedEntry.OldValue.Should().Be("InProgress");
        closedEntry.NewValue.Should().Be("Closed");
    }

    [Fact]
    public async Task AuditLogEntry_ShouldHaveUniqueIds()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        await _service.LogActionAsync(ticketId, "Action1", null);
        await _service.LogActionAsync(ticketId, "Action2", null);
        await _service.LogActionAsync(ticketId, "Action3", null);

        var entries = await _context.AuditLogs.ToListAsync();

        // Assert
        var ids = entries.Select(e => e.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().AllSatisfy(id => id.Should().NotBe(Guid.Empty));
    }

    #endregion
}
