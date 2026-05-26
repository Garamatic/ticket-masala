# Flexible ITicketModule Design — Deep Module Execution Gateway

> **Context:** .NET 10 ASP.NET Core | 15 legacy GERDA services | Domain events + observer pattern + outbox
> **Goal:** One stable interface that never changes when adding "ticket merge", "ticket split", "bulk import", or third-party plugins.

---

## 1. Interface Signature

### Philosophy: The Interface Is a Gateway, Not a Catalog

Instead of listing every operation (the current 13-method approach), the interface exposes **three execution primitives**: **Command**, **Query**, and **Context**. New features are new *types* plugged into the DI container — the interface surface stays frozen.

```csharp
using System.Security.Claims;
using TicketMasala.Domain.Events;

namespace TicketMasala.Web.Modules.Tickets;

// ─── Marker contracts (the "vocabulary" of the module) ───────────────────

/// <summary>Any operation that mutates ticket state.</summary>
public interface ITicketCommand<TResult> { }

/// <summary>Any read-only operation against ticket data.</summary>
public interface ITicketQuery<TResult> { }

/// <summary>Any domain event originating from the ticket module.</summary>
public interface ITicketDomainEvent : IDomainEvent { }

/// <summary>UI context payload (create form, edit form, detail page, etc.).</summary>
public interface ITicketContext { }

// ─── Operation context (cross-cutting data) ──────────────────────────────

/// <summary>
/// Carries ambient data that every pipeline middleware needs:
/// auth, correlation, feature flags, tenant, etc.
/// </summary>
public sealed record TicketOperationContext(
    ClaimsPrincipal? User = null,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, object>? Metadata = null)
{
    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public IReadOnlyList<string> Roles => User?.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList() ?? Array.Empty<string>();
    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}

// ─── The stable gateway interface (NEVER changes for new features) ─────

public interface ITicketModule
{
    // ─── Unified execution ─────────────────────────────────────────────

    /// <summary>
    /// Executes a state-mutating command through the full pipeline:
    /// validation → authorization → audit → handler → UoW commit →
    /// domain events → outbox → observers.
    /// </summary>
    Task<Result<TResult>> ExecuteAsync<TCommand, TResult>(
        TCommand command,
        TicketOperationContext ctx,
        CancellationToken ct = default)
        where TCommand : ITicketCommand<TResult>;

    /// <summary>
    /// Executes a read-only query through the read pipeline:
    /// authorization → caching → handler.
    /// </summary>
    Task<Result<TResult>> QueryAsync<TQuery, TResult>(
        TQuery query,
        TicketOperationContext ctx,
        CancellationToken ct = default)
        where TQuery : ITicketQuery<TResult>;

    // ─── UI context gateway ────────────────────────────────────────────

    /// <summary>
    /// Retrieves a typed UI context by discriminator key.
    /// Keys: "create", "edit", "detail", "search", "dispatch-backlog", etc.
    /// </summary>
    Task<Result<TContext>> GetContextAsync<TContext>(
        string contextKey,
        TicketContextRequest request,
        CancellationToken ct = default)
        where TContext : ITicketContext;

    /// <summary>
    /// Reload context after validation failures (minimal data, no heavy joins).
    /// </summary>
    Task<Result<TContext>> GetReloadContextAsync<TContext>(
        string contextKey,
        TicketContextRequest request,
        CancellationToken ct = default)
        where TContext : ITicketContext;

    // ─── Batch / bulk operations ────────────────────────────────────────

    /// <summary>
    /// Executes multiple commands in a single transaction.
    /// Failures are collected per item; successes are committed together.
    /// </summary>
    Task<BatchResult<TResult>> ExecuteBatchAsync<TCommand, TResult>(
        IReadOnlyList<TCommand> commands,
        TicketOperationContext ctx,
        BatchOptions? options = null,
        CancellationToken ct = default)
        where TCommand : ITicketCommand<TResult>;

    // ─── Cross-cutting control (for plugins & advanced callers) ────────

    /// <summary>
    /// Raises a ticket domain event explicitly (e.g., from external integrations).
    /// Bypasses the command pipeline; goes straight to dispatcher + outbox.
    /// </summary>
    Task RaiseEventAsync<TEvent>(
        TEvent @event,
        CancellationToken ct = default)
        where TEvent : ITicketDomainEvent;

    /// <summary>
    /// Subscribe to a ticket domain event at runtime (for dynamic plugins).
    /// Persistent handlers should be registered via DI; this is for ephemeral subscriptions.
    /// </summary>
    Task<IDisposable> SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        CancellationToken ct = default)
        where TEvent : ITicketDomainEvent;
}

// ─── Supporting types ──────────────────────────────────────────────────

public sealed record TicketContextRequest(
    Guid? TicketId = null,
    Guid? ProjectGuid = null,
    ClaimsPrincipal? User = null,
    IReadOnlyDictionary<string, object>? Params = null);

public sealed record BatchOptions(
    bool StopOnFirstFailure = false,
    bool NotifyObserversPerItem = true,
    int MaxConcurrency = 1); // default serial; callers opt-in to parallel

public sealed record BatchResult<TResult>
{
    public IReadOnlyList<BatchItemResult<TResult>> Items { get; init; } = Array.Empty<BatchItemResult<TResult>>();
    public int SuccessCount => Items.Count(i => i.Result.IsSuccess);
    public int FailureCount => Items.Count(i => i.Result.IsFailure);
    public bool AllSucceeded => Items.All(i => i.Result.IsSuccess);
    public IReadOnlyList<string> Errors => Items
        .Where(i => i.Result.IsFailure)
        .Select(i => $"[{i.Index}] {i.Result.Error}")
        .ToList();
}

public sealed record BatchItemResult<TResult>(int Index, Result<TResult> Result);

public static class TicketModuleExtensions
{
    /// <summary>Convenience overload when caller already has a ClaimsPrincipal.</summary>
    public static Task<Result<TResult>> ExecuteAsync<TCommand, TResult>(
        this ITicketModule module,
        TCommand command,
        ClaimsPrincipal user,
        CancellationToken ct = default)
        where TCommand : ITicketCommand<TResult>
        => module.ExecuteAsync(command, new TicketOperationContext(user), ct);

    public static Task<Result<TResult>> QueryAsync<TQuery, TResult>(
        this ITicketModule module,
        TQuery query,
        ClaimsPrincipal user,
        CancellationToken ct = default)
        where TQuery : ITicketQuery<TResult>
        => module.QueryAsync(query, new TicketOperationContext(user), ct);
}
```

### Pipeline Handler Contracts (registration surface)

Plugins and internal features register these via DI. The module resolves them from `IServiceProvider`.

```csharp
// ─── Handler contracts ─────────────────────────────────────────────────

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ITicketCommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, TicketOperationContext ctx, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : ITicketQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, TicketOperationContext ctx, CancellationToken ct);
}

public interface IContextHandler<TContext>
    where TContext : ITicketContext
{
    string Key { get; } // e.g., "create", "edit"
    Task<TContext> LoadAsync(TicketContextRequest request, CancellationToken ct);
    Task<TContext> LoadReloadAsync(TicketContextRequest request, CancellationToken ct);
}

// ─── Middleware contracts (cross-cutting, ordered by priority) ──────────

public interface ICommandMiddleware
{
    int Order { get; } // negative = early (auth), positive = late (events)
    Task<Result<TResult>> InvokeAsync<TCommand, TResult>(
        TCommand command,
        TicketOperationContext ctx,
        CommandMiddlewareDelegate<TResult> next,
        CancellationToken ct)
        where TCommand : ITicketCommand<TResult>;
}

public delegate Task<Result<TResult>> CommandMiddlewareDelegate<TResult>();
```

---

## 2. Usage Examples

### A. Current Controller (migrated from 13-method interface)

```csharp
public class TicketsController : Controller
{
    private readonly ITicketModule _module;

    public TicketsController(ITicketModule module) => _module = module;

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketViewModel vm)
    {
        var command = new CreateTicketCommand(
            vm.Description,
            vm.CustomerId,
            vm.ResponsibleId,
            vm.ProjectGuid,
            vm.CompletionTarget,
            vm.DomainId,
            vm.WorkItemTypeCode,
            vm.CustomFields,
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await _module.ExecuteAsync<CreateTicketCommand, Guid>(command, User);

        return result.IsSuccess
            ? RedirectToAction(nameof(Details), new { id = result.Value })
            : BadRequest(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Assign(AssignTicketViewModel vm)
    {
        var command = new AssignTicketCommand(vm.TicketId, vm.AgentId, User);
        var result = await _module.ExecuteAsync(command, User);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> CreateForm(Guid? projectGuid)
    {
        var ctx = await _module.GetContextAsync<TicketCreateContext>("create",
            new TicketContextRequest(ProjectGuid: projectGuid, User: User));

        return View(ctx.Value);
    }
}
```

### B. Adding "Ticket Merge" — Interface Does NOT Change

```csharp
// ─── New feature: zero changes to ITicketModule ────────────────────────

public sealed record MergeTicketsCommand(
    Guid SourceTicketId,
    Guid TargetTicketId,
    string MergedByUserId,
    bool PreserveComments = true,
    bool PreserveTimeLogs = true) : ITicketCommand<MergeResult>;

public sealed record MergeResult(
    Guid MergedTicketId,
    IReadOnlyList<Guid> ClosedTicketIds,
    int MigratedComments,
    int MigratedTimeLogs);

// Handler lives in its own file / assembly
public sealed class MergeTicketsHandler : ICommandHandler<MergeTicketsCommand, MergeResult>
{
    private readonly IUnitOfWork _uow;
    private readonly ITicketAuthorizationService _auth;
    public MergeTicketsHandler(IUnitOfWork uow, ITicketAuthorizationService auth) { ... }

    public async Task<MergeResult> HandleAsync(
        MergeTicketsCommand cmd,
        TicketOperationContext ctx,
        CancellationToken ct)
    {
        var source = await _uow.Tickets.GetByIdAsync(cmd.SourceTicketId, ct);
        var target = await _uow.Tickets.GetByIdAsync(cmd.TargetTicketId, ct);

        if (!_auth.CanMerge(source!, target!, ctx.UserId, ctx.Roles))
            throw new UnauthorizedAccessException("Not authorized to merge these tickets");

        // ... domain logic, UoW commit happens in pipeline after handler returns
        return new MergeResult(target!.Guid, new[] { source!.Guid }, 5, 3);
    }
}

// ─── Registration (Program.cs) ─────────────────────────────────────────

builder.Services.AddTransient<
    ICommandHandler<MergeTicketsCommand, MergeResult>,
    MergeTicketsHandler>();

// ─── Controller uses it immediately ──────────────────────────────────────

public async Task<IActionResult> Merge(MergeTicketsViewModel vm)
{
    var cmd = new MergeTicketsCommand(vm.SourceId, vm.TargetId, UserId);
    var result = await _module.ExecuteAsync(cmd, User);
    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
}
```

### C. Adding "Ticket Split" — Same Pattern

```csharp
public sealed record SplitTicketCommand(
    Guid OriginalTicketId,
    IReadOnlyList<SplitTicketFragment> Fragments) : ITicketCommand<SplitResult>;

public sealed record SplitTicketFragment(string Title, string Description, string? AssignToAgentId);
public sealed record SplitResult(IReadOnlyList<Guid> NewTicketIds, Guid ClosedOriginalId);

public sealed class SplitTicketHandler : ICommandHandler<SplitTicketCommand, SplitResult> { ... }
```

Register via DI. Interface unchanged.

### D. Batch Operations (Replacing TicketBatchService)

```csharp
var commands = ticketIds.Select(id =>
    new TransitionStatusCommand(id, "Pending", "Assigned", userId, roles));

var result = await _module.ExecuteBatchAsync(
    commands.ToList(),
    new TicketOperationContext(User),
    new BatchOptions(StopOnFirstFailure: false, MaxConcurrency: 4));

if (!result.AllSucceeded)
    _logger.LogWarning("Batch partial failure: {Errors}", result.Errors);
```

### E. Plugin / Extension Assembly (Third-Party)

```csharp
// In a separate NuGet package / assembly — no recompilation of core

public sealed record EscalateToSlackCommand(Guid TicketId, string Channel) : ITicketCommand<Unit>;

public sealed class EscalateToSlackHandler : ICommandHandler<EscalateToSlackCommand, Unit>
{
    public Task<Unit> HandleAsync(EscalateToSlackCommand cmd, TicketOperationContext ctx, CancellationToken ct)
    {
        // ... post to Slack
        return Task.FromResult(Unit.Value);
    }
}

// Plugin registers itself via IServiceCollection extension
public static class TicketModulePluginExtensions
{
    public static IServiceCollection AddSlackTicketPlugin(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<EscalateToSlackCommand, Unit>, EscalateToSlackHandler>();
        return services;
    }
}
```

### F. Dynamic Event Subscription (Runtime Plugins)

```csharp
// Subscribe to events without compile-time handler registration
await _module.SubscribeAsync<TicketCreatedEvent>(async (evt, ct) =>
{
    await _analytics.TrackAsync("ticket.created", evt.TicketId);
});
```

---

## 3. What Complexity It Hides Internally

The gateway implementation is ~1 file but orchestrates ~8 internal subsystems. Callers see a single `ExecuteAsync` line; internally:

| Hidden Subsystem | What the Pipeline Does | Current Code Location |
|---|---|---|
| **15 GERDA services** | Each command handler injects only the GERDA services it needs. The module does not know they exist. | `Engine/GERDA/Tickets/*.cs` |
| **Authorization matrix** | `AuthorizationMiddleware` runs before handler. Reads `[AuthorizeTicketOperation]` attributes or handler-implemented `IAuthorizedOperation`. No `if (!_auth.CanX)` in controllers. | Currently inline in `TicketModule.cs` |
| **Audit logging** | `AuditMiddleware` logs every command to `IAuditService` *after* success, with correlation ID. | Currently manual in each GERDA service |
| **Unit of work / transactions** | `UnitOfWorkMiddleware` wraps handler in `IUnitOfWork`. Commits on success, rolls back on failure. | Currently scattered `await _unitOfWork.CommitAsync()` |
| **Domain event dispatching** | `DomainEventMiddleware` flushes events via `IDomainEventDispatcher` after UoW commit. Replaces `DomainEventDispatchingInterceptor`. | `Data/DomainEventDispatchingInterceptor.cs` |
| **Outbox persistence** | `OutboxMiddleware` captures integration events and writes to `OutboxMessage` table if RabbitMQ is down. | `Services/OutboxPublisher.cs` |
| **Observer notification** | `ObserverMiddleware` iterates `IEnumerable<ITicketObserver>` and `IEnumerable<ICommentObserver>`, swallowing exceptions per-observer. Replaces manual loops. | Every GERDA service has its own `NotifyObserversAsync` |
| **Exception classification** | `ExceptionMappingMiddleware` maps `DomainException` → `Result.Failure`, `UnauthorizedAccessException` → 403, unexpected → 500 with sanitized message. | Currently repeated try/catch blocks in `TicketModule.cs` |
| **GERDA side effects** | `GerdaSideEffectsMiddleware` triggers AI dispatching, knowledge suggestions, notification emails — only for commands that opt-in via `IGerdaAffected`. | `TicketCreatedGerdaHandler.cs`, manual calls |
| **Batch orchestration** | `BatchMiddleware` manages transaction scope per batch, decides serial vs. parallel, aggregates partial failures, controls observer noise. | `TicketBatchService.cs` |
| **Query caching** | `QueryCacheMiddleware` short-circuits to `IMemoryCache` for context keys like `"create"` when parameters match. | Not currently implemented |
| **Validation** | `ValidationMiddleware` runs `IValidator<TCommand>` via FluentValidation or DataAnnotations before auth. | Currently in `CreateTicketCommand.Validate()` |

### Internal Pipeline Flow (One Diagram)

```
ExecuteAsync<MergeTicketsCommand, MergeResult>
  │
  ▼
[1] ValidationMiddleware    ──→ IValidator<MergeTicketsCommand>
  │                                    │
  ▼                                    ▼
[2] AuthorizationMiddleware ──→ ITicketAuthorizationService.CanMerge()
  │                                    │
  ▼                                    ▼
[3] UnitOfWorkMiddleware    ──→ BeginTransaction()
  │                                    │
  ▼                                    ▼
[4] Handler                 ──→ MergeTicketsHandler.HandleAsync()
  │         │                          │
  │         │         ┌────────────────┘
  │         │         │ (handler mutates aggregates)
  │         │         ▼
  │         │    Domain events queued on aggregates
  │         │         │
  │         ▼         ▼
  │      CommitAsync() ──→ EF SaveChanges
  │         │              │
  │         │              ▼
  │         │      DomainEventDispatchingInterceptor
  │         │      (legacy path — can be removed once all use module)
  │         │              │
  │         ▼              ▼
  │      (if success)  Dispatch domain events
  │         │              │
  │         ▼              ▼
  [5] DomainEventMiddleware ──→ IDomainEventDispatcher
  │                                    │
  ▼                                    ▼
  [6] OutboxMiddleware      ──→ OutboxPublisher (SQLite)
  │                                    │
  ▼                                    ▼
  [7] ObserverMiddleware    ──→ IEnumerable<ITicketObserver>
  │         │ (swallows exceptions per observer)
  │         │
  ▼         ▼
  [8] AuditMiddleware       ──→ IAuditService.LogActionAsync()
  │                                    │
  ▼                                    ▼
  [9] GerdaSideEffectsMiddleware ──→ IDispatchingService, IKnowledgeService
  │                                    │
  ▼                                    ▼
  Result<MergeResult> returned to caller
```

---

## 4. Dependency Strategy

### Category 1: Core Pipeline (Always Injected into Gateway)

```csharp
public sealed class TicketModule : ITicketModule
{
    private readonly IServiceProvider _services;      // Resolves handlers per-scope
    private readonly IEnumerable<ICommandMiddleware> _commandMiddleware;
    private readonly IEnumerable<IQueryMiddleware> _queryMiddleware;
    private readonly ILogger<TicketModule> _logger;
    private readonly ActivitySource _activitySource;   // .NET 10 OpenTelemetry
    // ...
}
```

These are **singleton or scoped** and never change. They form the execution chassis.

### Category 2: Cross-Cutting Middleware (Injected by DI, Resolved per Operation)

| Middleware | Dependencies | Lifetime |
|---|---|---|
| `ValidationMiddleware` | `IValidator<TCommand>` (generic, scoped) | Scoped |
| `AuthorizationMiddleware` | `ITicketAuthorizationService` | Scoped |
| `AuditMiddleware` | `IAuditService` | Scoped |
| `UnitOfWorkMiddleware` | `IUnitOfWork` | Scoped |
| `DomainEventMiddleware` | `IDomainEventDispatcher` | Scoped |
| `OutboxMiddleware` | `IOutboxPublisher` | Scoped |
| `ObserverMiddleware` | `IEnumerable<ITicketObserver>`, `IEnumerable<ICommentObserver>` | Scoped |
| `GerdaSideEffectsMiddleware` | `IDispatchingService`, `IKnowledgeService`, `IRabbitMqPublisher?` | Scoped |

These are **composed at startup** via `services.AddTicketModule()` and ordered by `Order` property.

### Category 3: Operation-Specific Handlers (Injected Only When Needed)

```csharp
// Each handler pulls exactly what it needs — no god constructor
public sealed class ResolveTicketHandler : ICommandHandler<ResolveTicketCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    private readonly ITicketResolutionService _resolution; // legacy GERDA, still usable
    private readonly ILogger<ResolveTicketHandler> _logger;
}
```

Handlers are **transient**. They hide GERDA service granularity from the module. Over time, GERDA services migrate *into* handlers or get decomposed further — the module doesn't care.

### Category 4: Context Handlers (UI-Specific)

```csharp
public sealed class TicketCreateContextHandler : IContextHandler<TicketCreateContext>
{
    public string Key => "create";
    private readonly ITicketCreateService _svc; // legacy GERDA service
    private readonly IDomainConfigurationService _domainConfig;
}
```

Context handlers are **discovered automatically** via assembly scanning at startup. New UI contexts (e.g., `"merge-preview"`) are just new `IContextHandler<T>` registrations.

### Category 5: External / Plugin Dependencies (Optional, Graceful Degradation)

```csharp
public sealed class GerdaSideEffectsMiddleware : ICommandMiddleware
{
    private readonly IDispatchingService? _dispatch;    // optional: null = disabled
    private readonly IRabbitMqPublisher? _rabbit;      // optional: null = outbox-only
    private readonly IKnowledgeService? _knowledge;    // optional: null = no KB suggestions
}
```

Plugins inject their own handlers and middleware. The pipeline skips missing optional services.

### DI Registration Extension (One Call for Consumers)

```csharp
// Program.cs
builder.Services.AddTicketModule(options =>
{
    // Pipeline order (defaults are sensible; override only if needed)
    options.UseMiddleware<ValidationMiddleware>(order: -100);
    options.UseMiddleware<AuthorizationMiddleware>(order: -50);
    options.UseMiddleware<UnitOfWorkMiddleware>(order: 0);
    options.UseMiddleware<DomainEventMiddleware>(order: 50);
    options.UseMiddleware<OutboxMiddleware>(order: 60);
    options.UseMiddleware<ObserverMiddleware>(order: 70);
    options.UseMiddleware<AuditMiddleware>(order: 80);
    options.UseMiddleware<GerdaSideEffectsMiddleware>(order: 100);

    // Scan current assembly + plugin assemblies for handlers
    options.ScanHandlers(typeof(Program).Assembly);
    options.ScanHandlers(typeof(SlackPlugin).Assembly);
});
```

---

## 5. Trade-Offs

### What You Lose

| Loss | Explanation | Mitigation |
|---|---|---|
| **IntelliSense discoverability** | You cannot press `.` on `ITicketModule` and see "CreateAsync, UpdateAsync, ResolveAsync…". You see `ExecuteAsync`, `QueryAsync`, `GetContextAsync`. | Provide a **typed facade** (`TicketModuleFacade` or extension methods) that wraps the gateway for the 90% use case. Keep the gateway for the 10% edge cases and plugins. |
| **Stack trace depth** | Every command goes through 5-9 middleware layers. Debugging a failure means stepping through `ValidationMiddleware → AuthMiddleware → UoWMiddleware → Handler → ...`. | Use `ActivitySource` / OpenTelemetry: each middleware creates a span. Traces show the full pipeline with timing per layer. |
| **Compile-time operation catalog** | The compiler cannot tell you "there is no handler registered for `MergeTicketsCommand`". You get a runtime `InvalidOperationException` when resolving `ICommandHandler<MergeTicketsCommand, MergeResult>`. | Add a **build-time analyzer** or **source generator** that scans for unregistered command/query types. Or use a startup validation pass: `services.ValidateTicketModuleHandlers()` throws during `Program.cs` if any `ITicketCommand<TResult>` lacks a handler. |
| **Indirection cost** | Generic type resolution + `IServiceProvider.GetService()` per operation adds ~1-2 µs + one dictionary lookup. | Negligible compared to DB round-trip (ms). For hot paths, use **compiled dispatch**: source generator emits a switch table `command.GetType() → handler`, eliminating reflection entirely. |
| **Learning curve** | New developers must understand middleware, handlers, and the marker interface pattern before writing a new ticket operation. | Provide a **scaffold template**: `dotnet new ticket-handler -n CloseTicket` generates handler, command, DI registration, and test stub. |
| **Over-flexibility risk** | A badly written handler can bypass UoW, forget to dispatch events, or call `_unitOfWork.CommitAsync()` directly (double-commit risk). | The `UnitOfWorkMiddleware` owns the transaction. Handlers receive a read-only `IUnitOfWork` facade that queues changes but cannot commit. Commit is middleware-controlled. |

### What You Gain

| Gain | Impact |
|---|---|
| **Open/Closed Principle** | New features = new types. Zero changes to `ITicketModule.cs`. Interface can be marked `sealed` in intent. |
| **Plugin architecture** | Third-party assemblies add commands/queries without recompiling core. NuGet packages extend the ticket system. |
| **Testability explosion** | Each handler is a pure function + dependencies. No need to mock `ITicketModule` with 13 methods; mock only the handler's 2-3 dependencies. |
| **Cross-cutting consistency** | Auth, audit, events, outbox, observers are applied **uniformly**. No more "oops, this new endpoint forgot to log audit trail." |
| **Batch as first-class** | `ExecuteBatchAsync` works for *any* command combination (mixed create + update + assign) because all share the same pipeline. |
| **Observability** | Middleware pipeline = natural tracing boundaries. Each layer emits its own span. Compare to today's monolithic `TicketModule.cs` where everything is one blob. |
| **Gradual migration** | Legacy GERDA services stay functional. Write new handlers that delegate to them, then refactor internally. No big-bang rewrite. |

---

## Concrete Migration Path (Opinionated)

1. **Week 1**: Create `ITicketModule` gateway + `TicketModule` implementation. Port existing 13 methods to handlers. Current `TicketModule.cs` becomes `TicketModuleFacade` (extension methods for discoverability).
2. **Week 2**: Migrate `TicketWorkflowService` → handlers (`CreateTicketHandler` delegates to `ITicketCreationService`, etc.). Mark `TicketWorkflowService` `[Obsolete(error: true)]`.
3. **Week 3**: Collapse observer loops into `ObserverMiddleware`. Remove `foreach (var observer in _observers)` from every GERDA service.
4. **Week 4**: Build `MergeTicketsHandler` as proof-of-concept that interface does not change. Write blog post / ADR.
5. **Month 2**: Source generator for compiled dispatch (`TicketModuleDispatch.g.cs`). Remove `IServiceProvider` runtime resolution from hot paths.

---

*Written for the ticket-masala deep module redesign. The interface surface is intentionally minimal; the power is in the type system and DI composition.*
