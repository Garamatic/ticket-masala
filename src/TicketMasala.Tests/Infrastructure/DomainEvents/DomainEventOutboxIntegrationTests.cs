using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Events;
using TicketMasala.Domain.Ports;
using TicketMasala.Domain.Tenancy;
using TicketMasala.Web.Data;
using TicketMasala.Web.Infrastructure.DomainEvents;

namespace TicketMasala.Web.Tests.Infrastructure.DomainEvents;

public class DomainEventOutboxIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public DomainEventOutboxIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns("test-tenant");
        services.AddSingleton(tenantMock.Object);

        services.AddScoped<DomainEventDispatchingInterceptor>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddTransient<IDomainEventContractMapper, TicketCreatedContractMapper>();
        services.AddTransient<IDomainEventContractMapper, TicketAssignedContractMapper>();
        services.AddTransient<IDomainEventContractMapper, TicketResolvedContractMapper>();

        services.AddDbContext<MasalaDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase($"OutboxTest_{Guid.NewGuid()}");
            options.EnableSensitiveDataLogging();
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatchingInterceptor>());
        }, ServiceLifetime.Scoped);

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    private MasalaDbContext CreateContext()
    {
        var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
    }

    private static Ticket ReadyForResolve(Ticket loaded)
    {
        ((IHasDomainEvents)loaded).ClearDomainEvents();
        loaded.TransitionTo(Status.InProgress, "u1");
        ((IHasDomainEvents)loaded).ClearDomainEvents();
        return loaded;
    }

    [Fact]
    public async Task CreateTicket_RaisesTicketCreatedEvent_OutboxRowCreated()
    {
        using var context = CreateContext();
        var ticket = Ticket.CreateFromPortal("Test ticket", "customer-1",
            completionTarget: DateTime.UtcNow.AddDays(7));
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var rows = await context.OutboxMessages.ToListAsync();
        Assert.Single(rows);
        Assert.Equal("ticket.created", rows[0].EventType);
        Assert.Equal("event.ticket.created", rows[0].RoutingKey);
        Assert.Contains("test-tenant", rows[0].Payload);
    }

    [Fact]
    public async Task EntityWithNoDomainEvents_NoOutboxRow()
    {
        using var context = CreateContext();
        context.Tickets.Add(new Ticket
        {
            Guid = Guid.NewGuid(), Title = "Test", Description = "Desc",
            CustomerId = "c1", DomainId = "IT",
            TicketStatus = Status.Pending, PriorityScore = 0,
            CustomFieldsJson = "{}", CreationDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        Assert.Empty(await context.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task ResolveTicket_RaisesTicketResolvedEvent_OutboxRowCreated()
    {
        using var context = CreateContext();
        var ticket = Ticket.CreateFromPortal("Ticket", "customer-1",
            completionTarget: DateTime.UtcNow.AddDays(7));
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var loaded = await context.Tickets.FirstAsync(t => t.Guid == ticket.Guid);
        loaded = ReadyForResolve(loaded);
        loaded.Resolve("Fixed", 100m, "u1");
        await context.SaveChangesAsync();

        var rows = await context.OutboxMessages.Where(m => m.EventType == "ticket.resolved").ToListAsync();
        Assert.Single(rows);
        Assert.Equal("event.ticket.resolved", rows[0].RoutingKey);
        Assert.Contains("Fixed", rows[0].Payload);
    }

    [Fact]
    public async Task AssignTicket_RaisesTicketAssignedEvent_OutboxRowCreated()
    {
        using var context = CreateContext();
        var ticket = Ticket.CreateFromPortal("Ticket", "customer-1",
            completionTarget: DateTime.UtcNow.AddDays(7));
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var loaded = await context.Tickets.FirstAsync(t => t.Guid == ticket.Guid);
        ((IHasDomainEvents)loaded).ClearDomainEvents();
        loaded.AssignTo("employee-42", "u1");
        await context.SaveChangesAsync();

        var rows = await context.OutboxMessages.Where(m => m.EventType == "ticket.assigned").ToListAsync();
        Assert.Single(rows);
        Assert.Equal("event.ticket.assigned", rows[0].RoutingKey);
    }

    [Fact]
    public async Task UnmappedEvent_DoesNotCreateOutboxRow()
    {
        using var context = CreateContext();
        var ticket = Ticket.CreateFromPortal("Ticket", "customer-1",
            completionTarget: DateTime.UtcNow.AddDays(7));
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var loaded = await context.Tickets.FirstAsync(t => t.Guid == ticket.Guid);
        ((IHasDomainEvents)loaded).ClearDomainEvents();
        loaded.UpdateTitle("New title", "u1");
        await context.SaveChangesAsync();

        var anyUpdated = await context.OutboxMessages.AnyAsync(
            m => m.EventType == "ticket-updated" || m.EventType == "ticket.updated");
        Assert.False(anyUpdated);
    }

    [Fact]
    public async Task MultipleOperations_CapturesMappedEventsAndSkipsUnmapped()
    {
        using var context = CreateContext();
        var ticket = Ticket.CreateFromPortal("Ticket", "customer-1",
            completionTarget: DateTime.UtcNow.AddDays(7));
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var loaded = await context.Tickets.FirstAsync(t => t.Guid == ticket.Guid);
        ((IHasDomainEvents)loaded).ClearDomainEvents();

        loaded.AssignTo("emp-1", "u1");
        loaded.TransitionTo(Status.InProgress, "u1");
        loaded.Resolve("Done", 200m, "u1");
        await context.SaveChangesAsync();

        var assigned = await context.OutboxMessages.CountAsync(m => m.EventType == "ticket.assigned");
        var resolved = await context.OutboxMessages.CountAsync(m => m.EventType == "ticket.resolved");
        var unmapped = await context.OutboxMessages.AnyAsync(
            m => m.EventType == "ticket-status-changed" || m.EventType == "ticket.status_changed");
        Assert.Equal(1, assigned);
        Assert.Equal(1, resolved);
        Assert.False(unmapped);
    }

    [Fact]
    public async Task SaveChangesFailed_DetachesOutboxRows()
    {
        var dbName = $"OutboxFail_{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddLogging();
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns("t");
        services.AddSingleton(tenantMock.Object);
        services.AddScoped<DomainEventDispatchingInterceptor>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddTransient<IDomainEventContractMapper, TicketCreatedContractMapper>();
        services.AddTransient<IDomainEventContractMapper, TicketResolvedContractMapper>();
        services.AddDbContext<MasalaDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(dbName);
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatchingInterceptor>());
        });
        using var sp = services.BuildServiceProvider();

        var knownGuid = Guid.NewGuid();
        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
            var ticket = Ticket.CreateFromPortal("Original", "c1");
            ticket.Guid = knownGuid;
            ctx.Tickets.Add(ticket);
            await ctx.SaveChangesAsync();
        }

        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
            var ticket = Ticket.CreateFromPortal("Duplicate", "c1");
            ticket.Guid = knownGuid;
            ctx.Tickets.Add(ticket);
            await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());

            var pending = ctx.ChangeTracker.Entries<OutboxMessage>()
                .Where(e => e.State == EntityState.Added).ToList();
            Assert.Empty(pending);
        }
    }

    [Fact]
    public async Task MapperEnrichesCustomerEmail_FromDbContext()
    {
        using var context = CreateContext();
        var customer = new ApplicationUser
        {
            Id = "cust-x", UserName = "cust-x",
            Email = "alice@example.com", FirstName = "Alice", LastName = "Johnson"
        };
        context.Users.Add(customer);
        await context.SaveChangesAsync();

        var ticket = Ticket.CreateFromPortal("Test ticket", "cust-x");
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var loaded = await context.Tickets.FirstAsync(t => t.Guid == ticket.Guid);
        loaded = ReadyForResolve(loaded);
        loaded.Resolve("Done", 100m, "u1");
        await context.SaveChangesAsync();

        var row = await context.OutboxMessages.FirstAsync(m => m.EventType == "ticket.resolved");
        Assert.Contains("alice@example.com", row.Payload);
        Assert.Contains("Alice Johnson", row.Payload);
    }

    [Fact]
    public async Task MapperEnrichesCustomerEmail_FromNavigationProperty()
    {
        using var context = CreateContext();
        var customer = new ApplicationUser
        {
            Id = "cust-y", UserName = "cust-y",
            Email = "bob@example.com", FirstName = "Bob", LastName = "Smith"
        };
        context.Users.Add(customer);

        var ticket = Ticket.CreateFromPortal("Test ticket", null);
        ticket.CustomerId = "cust-y";
        ticket.Customer = customer;
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var loaded = await context.Tickets.Include(t => t.Customer)
            .FirstAsync(t => t.Guid == ticket.Guid);
        loaded = ReadyForResolve(loaded);
        loaded.Resolve("Done", 100m, "u1");
        await context.SaveChangesAsync();

        var row = await context.OutboxMessages.FirstAsync(m => m.EventType == "ticket.resolved");
        Assert.Contains("bob@example.com", row.Payload);
        Assert.Contains("Bob Smith", row.Payload);
    }

    [Fact]
    public async Task InProcessHandler_DispatchedAfterSave()
    {
        var handlerCalled = false;
        var testHandler = new TestCreatedHandler(() => handlerCalled = true);

        var dbName = $"OutboxHandler_{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventHandler<TicketCreatedEvent>>(testHandler);
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<DomainEventDispatchingInterceptor>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddTransient<IDomainEventContractMapper, TicketCreatedContractMapper>();
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns("t");
        services.AddSingleton(tenantMock.Object);

        services.AddDbContext<MasalaDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(dbName);
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatchingInterceptor>());
        });
        using var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
        var ticket = Ticket.CreateFromPortal("Test", "c1");
        ctx.Tickets.Add(ticket);
        await ctx.SaveChangesAsync();

        Assert.True(handlerCalled);
    }

    private sealed class TestCreatedHandler : IDomainEventHandler<TicketCreatedEvent>
    {
        private readonly Action _onHandled;
        public TestCreatedHandler(Action onHandled) => _onHandled = onHandled;
        public Task HandleAsync(TicketCreatedEvent @event, CancellationToken ct = default)
        {
            _onHandled();
            return Task.CompletedTask;
        }
    }
}
