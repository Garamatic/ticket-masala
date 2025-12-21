# Observer Pattern

Documentation for the event-driven Observer pattern implementation in Ticket Masala.

## Overview

The Observer pattern enables **loose coupling** between ticket lifecycle events and various system responses. When a ticket is created, assigned, or completed, multiple observers can react independently without the core service needing to know about them.

```
TicketService.CreateAsync()
        ↓
    Notify All Observers
        ├── GerdaTicketObserver    → Queue AI processing
        ├── NotificationObserver   → Send user notifications
        └── LoggingObserver        → Audit logging
```

---

## Observer Interfaces

### ITicketObserver

Primary interface for ticket lifecycle events.

```csharp
public interface ITicketObserver
{
    Task OnTicketCreatedAsync(Ticket ticket);
    Task OnTicketAssignedAsync(Ticket ticket, Employee assignee);
    Task OnTicketCompletedAsync(Ticket ticket);
    Task OnTicketUpdatedAsync(Ticket ticket);
    Task OnTicketCommentedAsync(TicketComment comment);
}
```

### IProjectObserver

Interface for project lifecycle events.

```csharp
public interface IProjectObserver
{
    Task OnProjectCreatedAsync(Project project);
    Task OnProjectUpdatedAsync(Project project);
    Task OnProjectCompletedAsync(Project project);
    Task OnTicketAddedToProjectAsync(Project project, Ticket ticket);
}
```

### ICommentObserver

Interface for comment events.

```csharp
public interface ICommentObserver
{
    Task OnCommentCreatedAsync(TicketComment comment);
    Task OnCommentUpdatedAsync(TicketComment comment);
    Task OnCommentDeletedAsync(TicketComment comment);
}
```

---

## Built-in Observers

### GerdaTicketObserver

Triggers GERDA AI processing for new tickets.

**Location:** `Observers/GerdaTicketObserver.cs`

```csharp
public class GerdaTicketObserver : ITicketObserver
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        // Queue for background AI processing
        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var gerda = scope.ServiceProvider.GetRequiredService<IGerdaService>();
            await gerda.ProcessTicketAsync(ticket.Guid);
        });
    }
}
```

**Key Features:**
- Uses background queue for async processing
- Creates isolated DI scope for GERDA service
- Doesn't block the main request thread

---

### NotificationTicketObserver

Sends notifications to users when tickets change.

**Location:** `Observers/NotificationTicketObserver.cs`

```csharp
public class NotificationTicketObserver : ITicketObserver
{
    public async Task OnTicketAssignedAsync(Ticket ticket, Employee assignee)
    {
        await _notificationService.NotifyAsync(
            userId: assignee.Id,
            message: $"You have been assigned ticket: {ticket.Title}",
            type: NotificationType.Assignment
        );
    }
}
```

---

### LoggingTicketObserver

Creates audit log entries for ticket events.

**Location:** `Observers/LoggingTicketObserver.cs`

```csharp
public class LoggingTicketObserver : ITicketObserver
{
    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        _logger.LogInformation(
            "Ticket created: {TicketGuid} by {CustomerId}",
            ticket.Guid, ticket.CustomerId);
    }
}
```

---

## Registration

Observers are registered in `Extensions/ObserverExtensions.cs`:

```csharp
public static class ObserverExtensions
{
    public static IServiceCollection AddObservers(this IServiceCollection services)
    {
        // Register all ticket observers
        services.AddScoped<ITicketObserver, GerdaTicketObserver>();
        services.AddScoped<ITicketObserver, NotificationTicketObserver>();
        services.AddScoped<ITicketObserver, LoggingTicketObserver>();

        // Register project observers
        services.AddScoped<IProjectObserver, LoggingProjectObserver>();
        services.AddScoped<IProjectObserver, NotificationProjectObserver>();

        // Register comment observers
        services.AddScoped<ICommentObserver, CommentObservers>();

        return services;
    }
}
```

---

## Observer Invocation

The `TicketService` invokes observers after successful operations:

```csharp
public class TicketService : ITicketService
{
    private readonly IEnumerable<ITicketObserver> _observers;

    public async Task<Ticket> CreateTicketAsync(...)
    {
        // 1. Create the ticket
        var ticket = await _repository.AddAsync(newTicket);

        // 2. Notify all observers
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketCreatedAsync(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer failed");
                // Don't rethrow - observers shouldn't break the main flow
            }
        }

        return ticket;
    }
}
```

---

## Creating Custom Observers

### Step 1: Implement the Interface

```csharp
public class SlackNotificationObserver : ITicketObserver
{
    private readonly ISlackClient _slack;

    public SlackNotificationObserver(ISlackClient slack)
    {
        _slack = slack;
    }

    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        await _slack.PostMessageAsync(
            channel: "#support-tickets",
            message: $"New ticket: {ticket.Title}"
        );
    }

    // Implement other methods...
    public Task OnTicketAssignedAsync(Ticket ticket, Employee assignee) 
        => Task.CompletedTask;
    public Task OnTicketCompletedAsync(Ticket ticket) 
        => Task.CompletedTask;
    public Task OnTicketUpdatedAsync(Ticket ticket) 
        => Task.CompletedTask;
    public Task OnTicketCommentedAsync(TicketComment comment) 
        => Task.CompletedTask;
}
```

### Step 2: Register the Observer

```csharp
// In Extensions/ObserverExtensions.cs
services.AddScoped<ITicketObserver, SlackNotificationObserver>();

// Or in Program.cs
builder.Services.AddScoped<ITicketObserver, SlackNotificationObserver>();
```

---

## Best Practices

1. **Don't throw exceptions** - Observers should catch and log errors, not break the main flow
2. **Use background processing** - Long-running tasks should be queued
3. **Keep observers focused** - Each observer should have a single responsibility
4. **Avoid circular dependencies** - Observers shouldn't call back into services that trigger them
5. **Consider ordering** - If order matters, implement explicit priority

---

## Observer Execution Order

Observers are invoked in registration order. For explicit ordering:

```csharp
public interface IPrioritizedObserver
{
    int Priority { get; } // Lower = first
}

// In service:
foreach (var observer in _observers.OrderBy(o => 
    (o as IPrioritizedObserver)?.Priority ?? 100))
{
    await observer.OnTicketCreatedAsync(ticket);
}
```

---

## Testing Observers

```csharp
[Fact]
public async Task GerdaObserver_QueuesBackgroundTask()
{
    // Arrange
    var mockQueue = new Mock<IBackgroundTaskQueue>();
    var observer = new GerdaTicketObserver(mockQueue.Object, ...);
    var ticket = TestDataFactory.CreateTicket();

    // Act
    await observer.OnTicketCreatedAsync(ticket);

    // Assert
    mockQueue.Verify(q => 
        q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, Task>>()),
        Times.Once);
}
```

---

## Further Reading

- [Architecture Overview](SUMMARY.md)
- [GERDA AI Documentation](gerda-ai/GERDA_MODULES.md)
- [Testing Guide](../guides/TESTING.md)
