using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Enums;
using TicketMasala.Domain.Repositories;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using TicketMasala.Web.Engine.Security;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Tests.Fixtures;
using Xunit;

namespace TicketMasala.Tests.Engine.GERDA.Tickets.Lifecycle;

public class TicketLifecycleTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<ITicketObserver> _ticketObserver = new();
    private readonly Mock<ICommentObserver> _commentObserver = new();
    private readonly Mock<IPiiScrubberService> _scrubber = new();
    private readonly Mock<ISystemClock> _clock = new();
    private readonly MasalaDbContext _dbContext;

    public TicketLifecycleTests()
    {
        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new MasalaDbContext(options);

        _uow.SetupGet(x => x.Tickets).Returns(new EfCoreTicketRepository(_dbContext, new NullLogger<EfCoreTicketRepository>(), _clock.Object));
        _uow.SetupGet(x => x.Projects).Returns(new EfCoreProjectRepository(_dbContext, new NullLogger<EfCoreProjectRepository>()));
        _uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uow.Setup(x => x.AddCommentAsync(It.IsAny<TicketComment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(x => x.AddTimeLogAsync(It.IsAny<TimeLog>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(x => x.AddQualityReviewAsync(It.IsAny<QualityReview>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(x => x.AddOutboxMessageAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _scrubber.Setup(x => x.Scrub(It.IsAny<string>())).Returns<string>(s => s);
        _clock.SetupGet(x => x.UtcNow).Returns(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    private TicketLifecycle CreateSUT(IEnumerable<ITicketObserver>? ticketObservers = null,
        IEnumerable<ICommentObserver>? commentObservers = null)
    {
        return new TicketLifecycle(
            _uow.Object,
            _users.Object,
            _audit.Object,
            ticketObservers ?? new[] { _ticketObserver.Object },
            commentObservers ?? new[] { _commentObserver.Object },
            _scrubber.Object,
            _clock.Object,
            new NullLogger<TicketLifecycle>());
    }

    private TicketContext Ctx(string userId = "user-123") => new(userId);

    // ═════════════════════════════════════════════════════════════════
    // CREATE
    // ═════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateTicket_WithValidData_CreatesTicketAndAuditAndNotifies()
    {
        var customer = new UserBuilder().Build();
        _users.Setup(x => x.GetCustomerByIdAsync(customer.Id)).ReturnsAsync(customer);

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new CreateTicketCommand("Need help with network", customer.Id),
            Ctx());

        Assert.True(result.Success);
        Assert.NotNull(result.Ticket);
        Assert.Equal("Need help with network", result.Ticket!.Description);
        Assert.Equal(Status.Pending, result.Ticket.TicketStatus);

        _audit.Verify(x => x.LogActionAsync(result.Ticket.Guid, "Created", "user-123", null, null, null), Times.Once);
        _uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _ticketObserver.Verify(x => x.OnTicketUpdatedAsync(It.Is<Ticket>(t => t.Guid == result.Ticket.Guid)), Times.Once);
    }

    [Fact]
    public async Task CreateTicket_WithResponsible_SetsStatusToAssigned()
    {
        var customer = new UserBuilder().Build();
        var employee = new Employee { Id = "emp-1", UserName = "emp@test.com", Email = "emp@test.com", FirstName = "Alice", LastName = "Smith" };
        _users.Setup(x => x.GetCustomerByIdAsync(customer.Id)).ReturnsAsync(customer);
        _users.Setup(x => x.GetEmployeeByIdAsync(employee.Id)).ReturnsAsync(employee);

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new CreateTicketCommand("Assigned ticket", customer.Id, employee.Id),
            Ctx());

        Assert.True(result.Success);
        Assert.Equal(Status.Assigned, result.Ticket!.TicketStatus);
        Assert.Equal(employee.Id, result.Ticket.ResponsibleId);
        _ticketObserver.Verify(x => x.OnTicketAssignedAsync(It.IsAny<Ticket>(), It.Is<Employee>(e => e.Id == employee.Id)), Times.Once);
    }

    [Fact]
    public async Task CreateTicket_WithMissingCustomer_ReturnsFail()
    {
        _users.Setup(x => x.GetCustomerByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new CreateTicketCommand("No customer", "missing-id"),
            Ctx());

        Assert.False(result.Success);
        Assert.Contains("Customer not found", result.ErrorMessage);
    }

    // ═════════════════════════════════════════════════════════════════
    // RESOLVE
    // ═════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveTicket_WithValidData_ResolvesAndQueuesOutboxMessage()
    {
        var ticket = new TicketBuilder().WithStatus(Status.InProgress).Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new ResolveTicketCommand(ticket.Guid, "Fixed the network issue", 150.00m),
            Ctx());

        Assert.True(result.Success);
        Assert.Equal(Status.Completed, result.Ticket!.TicketStatus);
        Assert.Equal("Fixed the network issue", result.Ticket.ResolutionNotes);
        Assert.Equal(150.00m, result.Ticket.BillableAmount);

        _audit.Verify(x => x.LogActionAsync(ticket.Guid, "Resolved", "user-123", null, null, It.IsAny<string>()), Times.Once);
        _uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _ticketObserver.Verify(x => x.OnTicketUpdatedAsync(It.Is<Ticket>(t => t.Guid == ticket.Guid)), Times.Once);
        _ticketObserver.Verify(x => x.OnTicketCompletedAsync(It.Is<Ticket>(t => t.Guid == ticket.Guid)), Times.Once);
        _uow.Verify(x => x.AddOutboxMessageAsync(
            It.Is<OutboxMessage>(m =>
                m.EventType == "ticket.resolved" &&
                m.RoutingKey == "event.ticket.resolved"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveTicket_WithMissingTicket_ReturnsFail()
    {
        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new ResolveTicketCommand(Guid.NewGuid(), "Notes", 100m),
            Ctx());

        Assert.False(result.Success);
        Assert.Contains("Ticket not found", result.ErrorMessage);
        _uow.Verify(x => x.AddOutboxMessageAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveTicket_WithEmptyNotes_ReturnsFail()
    {
        var ticket = new TicketBuilder().WithStatus(Status.InProgress).Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new ResolveTicketCommand(ticket.Guid, "", 100m),
            Ctx());

        Assert.False(result.Success);
        _uow.Verify(x => x.AddOutboxMessageAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveTicket_QueuesOutboxMessageWithCorrectSchema()
    {
        var ticket = new TicketBuilder().WithStatus(Status.InProgress).Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        OutboxMessage? capturedMessage = null;
        _uow.Setup(x => x.AddOutboxMessageAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new ResolveTicketCommand(ticket.Guid, "Notes", 100m),
            Ctx());

        Assert.True(result.Success);
        Assert.NotNull(capturedMessage);
        Assert.Equal("ticket.resolved", capturedMessage!.EventType);
        Assert.Equal("event.ticket.resolved", capturedMessage.RoutingKey);
        Assert.Contains("ticket_id", capturedMessage.Payload);
        Assert.Contains("customer_email", capturedMessage.Payload);
        Assert.Contains("timestamp", capturedMessage.Payload);
        Assert.Contains("source", capturedMessage.Payload);
        Assert.Contains("ticket-masala", capturedMessage.Payload);
    }

    // ═════════════════════════════════════════════════════════════════
    // COMMENT
    // ═════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddComment_CreatesCommentAndNotifiesBothObservers()
    {
        var ticket = new TicketBuilder().Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new AddCommentCommand(ticket.Guid, "Looking into this", false),
            Ctx("agent-1"));

        Assert.True(result.Success);
        Assert.NotNull(result.Comment);
        Assert.Equal("Looking into this", result.Comment!.Body);
        Assert.Equal("agent-1", result.Comment.AuthorId);

        _audit.Verify(x => x.LogActionAsync(ticket.Guid, "Commented", "agent-1", null, null, It.IsAny<string>()), Times.Once);
        _ticketObserver.Verify(x => x.OnTicketCommentedAsync(It.Is<TicketComment>(c => c.Body == "Looking into this")), Times.Once);
        _commentObserver.Verify(x => x.OnCommentAddedAsync(It.Is<TicketComment>(c => c.Body == "Looking into this")), Times.Once);
    }

    // ═════════════════════════════════════════════════════════════════
    // TIME LOG
    // ═════════════════════════════════════════════════════════════════

    [Fact]
    public async Task LogTime_CreatesTimeLog()
    {
        var ticket = new TicketBuilder().Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new LogTimeCommand(ticket.Guid, 2.5, new DateTime(2024, 1, 10), "Investigated issue"),
            Ctx("agent-1"));

        Assert.True(result.Success);
        Assert.NotNull(result.TimeLog);
        Assert.Equal(2.5, result.TimeLog!.Hours);
        _audit.Verify(x => x.LogActionAsync(ticket.Guid, "TimeLogged", "agent-1", null, null, "2.5 hours"), Times.Once);
    }

    [Fact]
    public async Task LogTime_WithZeroHours_ReturnsFail()
    {
        var ticket = new TicketBuilder().Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new LogTimeCommand(ticket.Guid, 0, DateTime.UtcNow, "Nothing"),
            Ctx());

        Assert.False(result.Success);
        Assert.Contains("greater than zero", result.ErrorMessage!);
    }

    // ═════════════════════════════════════════════════════════════════
    // ASSIGN
    // ═════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AssignTicket_WithAgent_AssignsAndNotifies()
    {
        var ticket = new TicketBuilder().Build();
        var employee = new Employee { Id = "emp-1", UserName = "emp@test.com", Email = "emp@test.com", FirstName = "Alice", LastName = "Smith" };
        _users.Setup(x => x.GetEmployeeByIdAsync(employee.Id)).ReturnsAsync(employee);

        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new AssignTicketCommand(ticket.Guid, employee.Id),
            Ctx("manager-1"));

        Assert.True(result.Success);
        Assert.Equal(employee.Id, result.Ticket!.ResponsibleId);
        Assert.Equal(Status.Assigned, result.Ticket.TicketStatus);
        _ticketObserver.Verify(x => x.OnTicketAssignedAsync(It.IsAny<Ticket>(), It.Is<Employee>(e => e.Id == employee.Id)), Times.Once);
    }

    // ═════════════════════════════════════════════════════════════════
    // REVIEW
    // ═════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RequestReview_SetsPendingStatus()
    {
        var ticket = new TicketBuilder().Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new RequestReviewCommand(ticket.Guid),
            Ctx("requester-1"));

        Assert.True(result.Success);
        Assert.Equal(ReviewStatus.Pending, result.Ticket!.ReviewStatus);
        _audit.Verify(x => x.LogActionAsync(ticket.Guid, "ReviewRequested", "requester-1", null, null, null), Times.Once);
    }

    [Fact]
    public async Task SubmitReview_CreatesReviewRecord()
    {
        var ticket = new TicketBuilder().Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new SubmitReviewCommand(ticket.Guid, 8, "Good work", true),
            Ctx("reviewer-1"));

        Assert.True(result.Success);
        Assert.Equal(ReviewStatus.Approved, result.Ticket!.ReviewStatus);
        _audit.Verify(x => x.LogActionAsync(ticket.Guid, "ReviewApproved", "reviewer-1", "QualityReview", null, "Good work"), Times.Once);
        _uow.Verify(x => x.AddQualityReviewAsync(It.Is<QualityReview>(r => r.Score == 8 && r.IsApproved), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═════════════════════════════════════════════════════════════════
    // BATCH
    // ═════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BatchAssign_AssignsMultipleTickets()
    {
        var t1 = new TicketBuilder().Build();
        var t2 = new TicketBuilder().Build();
        var employee = new Employee { Id = "emp-1", UserName = "emp@test.com", Email = "emp@test.com", FirstName = "Alice", LastName = "Smith" };
        _users.Setup(x => x.GetEmployeeByIdAsync(employee.Id)).ReturnsAsync(employee);

        await _dbContext.Tickets.AddRangeAsync(t1, t2);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new BatchAssignCommand(new[] { t1.Guid, t2.Guid }, employee.Id),
            Ctx("manager-1"));

        Assert.True(result.Success);
        _ticketObserver.Verify(x => x.OnTicketAssignedAsync(It.IsAny<Ticket>(), It.Is<Employee>(e => e.Id == employee.Id)), Times.Exactly(2));
    }

    // ═════════════════════════════════════════════════════════════════
    // RESILIENCE
    // ═════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResolveTicket_WhenObserverThrows_CommandStillSucceeds()
    {
        var ticket = new TicketBuilder().WithStatus(Status.InProgress).Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var throwingObserver = new Mock<ITicketObserver>();
        throwingObserver.Setup(x => x.OnTicketUpdatedAsync(It.IsAny<Ticket>())).ThrowsAsync(new InvalidOperationException("Observer crash"));

        var sut = CreateSUT(ticketObservers: new[] { throwingObserver.Object });
        var result = await sut.ExecuteAsync(
            new ResolveTicketCommand(ticket.Guid, "Notes", 100m),
            Ctx());

        Assert.True(result.Success);
        _uow.Verify(x => x.AddOutboxMessageAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveTicket_AlwaysQueuesOutboxMessage()
    {
        var ticket = new TicketBuilder().WithStatus(Status.InProgress).Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        var result = await sut.ExecuteAsync(
            new ResolveTicketCommand(ticket.Guid, "Notes", 100m),
            Ctx());

        Assert.True(result.Success);
        _uow.Verify(x => x.AddOutboxMessageAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveTicket_CommitsOnce()
    {
        var ticket = new TicketBuilder().WithStatus(Status.InProgress).Build();
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var sut = CreateSUT();
        await sut.ExecuteAsync(
            new ResolveTicketCommand(ticket.Guid, "Notes", 100m),
            Ctx());

        _uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
