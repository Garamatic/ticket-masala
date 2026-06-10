using Microsoft.EntityFrameworkCore;
using Moq;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Events;
using TicketMasala.Domain.Tenancy;
using TicketMasala.Web.Infrastructure.DomainEvents;

namespace TicketMasala.Web.Tests.Infrastructure.DomainEvents;

public class ContractMapperTests
{
    private static MasalaDbContext CreateMockContext()
    {
        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new Mock<MasalaDbContext>(options) { CallBase = true }.Object;
    }

    [Fact]
    public void TicketCreatedMapper_ReturnsNull_ForWrongEventType()
    {
        var mapper = new TicketCreatedContractMapper(
            CreateMockContext(),
            Mock.Of<ITenantContext>());
        Assert.Null(mapper.Map(Mock.Of<IDomainEvent>()));
    }

    [Fact]
    public void TicketCreatedMapper_MapsRequiredFields()
    {
        var domainEvent = new TicketCreatedEvent(
            ticketGuid: Guid.NewGuid(),
            customerId: "cust-1",
            domainId: "IT");

        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns("desgoffe");

        var mapper = new TicketCreatedContractMapper(
            CreateMockContext(),
            tenantMock.Object);

        var result = mapper.Map(domainEvent);

        Assert.NotNull(result);
        Assert.Equal("ticket.created", result!.EventType);
        Assert.Equal("event.ticket.created", result.RoutingKey);
        Assert.IsType<RabbitMqConnector.Contracts.TicketCreatedEvent>(result.Payload);

        var payload = (RabbitMqConnector.Contracts.TicketCreatedEvent)result.Payload;
        Assert.Equal(domainEvent.TicketGuid.ToString(), payload.TicketId);
        Assert.Equal("desgoffe", payload.TenantId);
        Assert.Equal(domainEvent.EventId.ToString(), payload.EventId);
    }

    [Fact]
    public void TicketAssignedMapper_ReturnsNull_ForWrongEventType()
    {
        var mapper = new TicketAssignedContractMapper(CreateMockContext());
        Assert.Null(mapper.Map(Mock.Of<IDomainEvent>()));
    }

    [Fact]
    public void TicketAssignedMapper_MapsRequiredFields()
    {
        var domainEvent = new TicketAssignedEvent(
            ticketGuid: Guid.NewGuid(),
            newResponsibleId: "emp-42",
            oldResponsibleId: null,
            assignedByUserId: "user-1");

        var mapper = new TicketAssignedContractMapper(CreateMockContext());
        var result = mapper.Map(domainEvent);

        Assert.NotNull(result);
        Assert.Equal("ticket.assigned", result!.EventType);
        Assert.Equal("event.ticket.assigned", result.RoutingKey);
        Assert.IsType<RabbitMqConnector.Contracts.TicketAssignedEvent>(result.Payload);

        var payload = (RabbitMqConnector.Contracts.TicketAssignedEvent)result.Payload;
        Assert.Equal(domainEvent.TicketGuid.ToString(), payload.TicketId);
        Assert.Contains(domainEvent.NewResponsibleId, payload.AssignedTo);
    }

    [Fact]
    public void TicketResolvedMapper_ReturnsNull_ForWrongEventType()
    {
        var mapper = new TicketResolvedContractMapper(
            CreateMockContext(),
            Mock.Of<ITenantContext>());
        Assert.Null(mapper.Map(Mock.Of<IDomainEvent>()));
    }

    [Fact]
    public void TicketResolvedMapper_MapsRequiredFields()
    {
        var now = DateTime.UtcNow;
        var domainEvent = new TicketResolvedEvent(
            ticketGuid: Guid.NewGuid(),
            customerId: "cust-1",
            billableAmount: 150.00m,
            resolutionNotes: "Fixed the issue",
            resolvedAt: now,
            resolvedByUserId: "user-1");

        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns("desgoffe");

        var mapper = new TicketResolvedContractMapper(
            CreateMockContext(),
            tenantMock.Object);
        var result = mapper.Map(domainEvent);

        Assert.NotNull(result);
        Assert.Equal("ticket.resolved", result!.EventType);
        Assert.Equal("event.ticket.resolved", result.RoutingKey);
        Assert.IsType<RabbitMqConnector.Contracts.TicketResolvedEvent>(result.Payload);

        var payload = (RabbitMqConnector.Contracts.TicketResolvedEvent)result.Payload;
        Assert.Equal(domainEvent.TicketGuid.ToString(), payload.TicketId);
        Assert.Equal(150.00m, payload.Amount);
        Assert.Equal("Fixed the issue", payload.ResolutionNotes);
        Assert.Equal("desgoffe", payload.TenantId);
    }
}
