# Minimal TicketModule Interface Design

> **Constraint:** 1–3 public entry points. Everything else is internal to the deep module.
> **Target:** C# 13 / .NET 10 ASP.NET Core
> **Status:** Design proposal — ready for implementation

---

## 1. Interface Signature

Two methods. That's it. One for writes, one for reads. The command/query objects themselves carry the return-type contract via the generic constraint.

```csharp
using System.Security.Claims;
using TicketMasala.Web.Common;

namespace TicketMasala.Web.Modules.Tickets;

/// <summary>
/// Deep module facade for the entire ticket domain.
/// Hides 15+ legacy GERDA services, observer orchestration, outbox publishing,
/// audit logging, authorization, and domain-event dispatch behind two methods.
/// </summary>
public interface ITicketModule
{
    /// <summary>
    /// Executes any ticket write operation (create, update, assign, transition,
    /// comment, resolve, review, time-log, batch-assign, batch-status-change).
    /// Authorization, audit, observers, outbox, and GERDA processing are handled internally.
    /// </summary>
    Task<Result<TResult>> ExecuteAsync<TResult>(
        ITicketCommand<TResult> command,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    /// <summary>
    /// Executes any ticket read operation (details, search, UI contexts, AI summary,
    /// dispatch backlog). Authorization and customer-scoping are handled internally.
    /// </summary>
    Task<TResult> QueryAsync<TResult>(
        ITicketQuery<TResult> query,
        ClaimsPrincipal user,
        CancellationToken ct = default);
}

// ─── Marker interfaces for the discriminated-union pattern ─────────────────

/// <summary>
/// Marker for a command that mutates state and returns <typeparamref name="TResult"/>.
/// </summary>
public interface ITicketCommand<TResult> { }

/// <summary>
/// Marker for a query that reads state and returns <typeparamref name="TResult"/>.
/// </summary>
public interface ITicketQuery<TResult> { }
```

### Why two methods instead of one?

A single `HandleAsync` would force every query through `Result<T>` wrapping and make the API surface semantically muddy (reads don't "fail" the same way writes do — they return empty or throw). Two methods preserves CQRS semantics without expanding the surface. Any fewer would cost clarity; any more would violate the minimality constraint.

---

## 2. Command & Query Types (The Real API)

The interface is useless without the concrete request types. These are the actual public API surface for consumers. They replace the 13+ methods currently on `ITicketModule` and the ~15 legacy GERDA service interfaces.

### Write Commands (all implement `ITicketCommand<TResult>`)

```csharp
// ─── Core lifecycle ───────────────────────────────────────────────────────────

public sealed record CreateTicketCommand(
    string Description,
    string CustomerId,
    string? ResponsibleId,
    Guid? ProjectGuid,
    DateTime? CompletionTarget,
    string? DomainId,
    string? WorkItemTypeCode,
    Dictionary<string, string> CustomFields,
    string CreatedByUserId
) : ITicketCommand<Result<Guid>>;

public sealed record UpdateTicketCommand(
    Guid TicketId,
    string Description,
    string TicketStatus,
    DateTime? CompletionTarget,
    string? CustomerId,
    Guid? ProjectGuid,
    Dictionary<string, string> CustomFields,
    string ModifiedByUserId,
    IReadOnlyList<string> ModifiedByRoles
) : ITicketCommand<Result<Unit>>;

public sealed record AssignTicketCommand(
    Guid TicketId,
    string ResponsibleId,
    string AssignedByUserId,
    IReadOnlyList<string> AssignedByRoles
) : ITicketCommand<Result<Unit>>;

public sealed record TransitionStatusCommand(
    Guid TicketId,
    string FromStatus,
    string ToStatus,
    string ChangedByUserId,
    IReadOnlyList<string> ChangedByRoles
) : ITicketCommand<Result<Unit>>;

// ─── Operations previously in legacy GERDA services ───────────────────────

public sealed record AddCommentCommand(
    Guid TicketId,
    string Body,
    bool IsInternal,
    string AuthorId
) : ITicketCommand<Result<Unit>>;

public sealed record ResolveTicketCommand(
    Guid TicketId,
    string ResolutionNotes,
    decimal? BillableAmount,
    string ResolvedByUserId
) : ITicketCommand<Result<Unit>>;

public sealed record RequestReviewCommand(
    Guid TicketId,
    string RequestedByUserId,
    string? ReviewerUserId,
    string? Instructions
) : ITicketCommand<Result<Unit>>;

public sealed record SubmitReviewCommand(
    Guid TicketId,
    string ReviewerUserId,
    int QualityScore,
    string? Feedback,
    bool Approved
) : ITicketCommand<Result<Unit>>;

public sealed record LogTimeCommand(
    Guid TicketId,
    string UserId,
    double Hours,
    DateTime Date,
    string? Description,
    bool IsBillable
) : ITicketCommand<Result<Unit>>;

public sealed record BatchAssignCommand(
    IReadOnlyList<Guid> TicketIds,
    string AgentId,
    string? AssignedByUserId
) : ITicketCommand<Result<BatchOperationResult>>;

public sealed record BatchUpdateStatusCommand(
    IReadOnlyList<Guid> TicketIds,
    string TargetStatus,
    string ChangedByUserId
) : ITicketCommand<Result<BatchOperationResult>>;

// ─── Shared result for batch ops ────────────────────────────────────────────

public sealed record BatchOperationResult(
    int Succeeded,
    int Failed,
    IReadOnlyList<BatchFailure> Failures);

public sealed record BatchFailure(Guid TicketId, string Reason);
```

### Read Queries (all implement `ITicketQuery<TResult>`)

```csharp
// ─── Core queries ───────────────────────────────────────────────────────────

public sealed record GetTicketDetailsQuery(Guid TicketId)
    : ITicketQuery<Result<TicketDetailsDto>>;

public sealed record SearchTicketsQuery(
    string? SearchTerm,
    string? Status,
    string? CustomerId,
    Guid? ProjectGuid,
    string? ResponsibleId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 20
) : ITicketQuery<TicketSearchResult>;

// ─── UI context queries (previously 7 separate methods) ─────────────────────

public sealed record SearchTicketsForUiQuery(TicketSearchViewModel? Model)
    : ITicketQuery<TicketSearchViewModel>;

public sealed record GetDetailPageQuery(Guid TicketId)
    : ITicketQuery<(TicketDetailsViewModel? ViewModel, TicketDetailContext Context)>;

public sealed record GenerateAiSummaryQuery(Guid TicketId)
    : ITicketQuery<string>;

public sealed record GetCreateContextQuery(Guid? ProjectGuid)
    : ITicketQuery<TicketCreateContext>;

public sealed record GetEditContextQuery(Guid TicketId)
    : ITicketQuery<TicketEditContext?>;

public sealed record GetCreateReloadContextQuery(Guid? ProjectGuid)
    : ITicketQuery<TicketCreateContext>;

public sealed record GetEditReloadContextQuery(Guid TicketId)
    : ITicketQuery<TicketEditContext>;

// ─── GERDA dispatch backlog (currently orphaned in IDispatchBacklogService) ─

public sealed record GetDispatchBacklogQuery(int Page = 1, int PageSize = 20)
    : ITicketQuery<GerdaDispatchViewModel>;
```

---

## 3. Usage Example (Controller / Minimal API)

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketMasala.Web.Common;
using TicketMasala.Web.Modules.Tickets;

namespace TicketMasala.Web.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController(ITicketModule tickets) : ControllerBase
{
    // ─── Writes ─────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateTicketCommand cmd,
        CancellationToken ct)
    {
        // The module performs authorization internally; no [Authorize] attribute soup.
        var result = await tickets.ExecuteAsync(cmd, User, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult> AddComment(
        Guid id,
        [FromBody] AddCommentBody body,
        CancellationToken ct)
    {
        var cmd = new AddCommentCommand(id, body.Body, body.IsInternal, User.Identity!.Name!);
        var result = await tickets.ExecuteAsync(cmd, User, ct);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult> Resolve(
        Guid id,
        [FromBody] ResolveTicketBody body,
        CancellationToken ct)
    {
        var cmd = new ResolveTicketCommand(id, body.Notes, body.BillableAmount, User.Identity!.Name!);
        var result = await tickets.ExecuteAsync(cmd, User, ct);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpPost("batch/assign")]
    public async Task<ActionResult<BatchOperationResult>> BatchAssign(
        [FromBody] BatchAssignCommand cmd,
        CancellationToken ct)
    {
        var result = await tickets.ExecuteAsync(cmd, User, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // ─── Reads ──────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDetailsDto>> GetDetails(Guid id, CancellationToken ct)
    {
        var result = await tickets.QueryAsync(new GetTicketDetailsQuery(id), User, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet]
    public async Task<ActionResult<TicketSearchResult>> Search(
        [FromQuery] SearchTicketsQuery query,
        CancellationToken ct)
    {
        // No manual customer scoping here — the module applies it internally
        // based on the ClaimsPrincipal roles.
        var result = await tickets.QueryAsync(query, User, ct);
        return Ok(result);
    }

    [HttpGet("dispatch-backlog")]
    public async Task<ActionResult<GerdaDispatchViewModel>> DispatchBacklog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var vm = await tickets.QueryAsync(new GetDispatchBacklogQuery(page, pageSize), User, ct);
        return Ok(vm);
    }
}
```

---

## 4. What Complexity the Module Hides

| Hidden Concern | Current State (Leaking) | Deep Module Behavior |
|---|---|---|
| **Legacy GERDA service orchestration** | 15+ services (`TicketCommentService`, `TicketResolutionService`, `TicketReviewService`, `TicketTimeLoggingService`, `TicketBatchService`, `TicketAssignmentFacade`, `TicketDispatchService`, etc.) are injected individually into controllers or call each other ad-hoc. | All internal. The module's `ExecuteAsync` routes to a private command-handler dispatch table (switch on `command.GetType()` or a frozen dictionary of delegates). |
| **Observer pattern (9 implementations)** | Services manually `foreach` over `IEnumerable<ITicketObserver>`, `ICommentObserver`, `IProjectObserver`, catching exceptions per observer. | The module owns observer iteration. After `SaveChanges` succeeds, it dispatches to all observers. Observer failures are logged and swallowed — they never bubble up to the caller. |
| **Domain event dispatch** | `DomainEventDispatchingInterceptor` (EF Core interceptor) captures events, then resolves `IDomainEventHandler<T>` via reflection + `IServiceProvider` after `SaveChanges`. | Still used internally, but **invisible** to consumers. The module may replace the interceptor with an explicit `PublishDomainEventsAsync()` call inside `ExecuteAsync` to make the ordering deterministic. |
| **Outbox pattern** | `OutboxPublisher` polls SQLite and publishes to RabbitMQ independently. Services sometimes double-publish directly to RabbitMQ when the outbox isn't enough. | All writes that need integration events queue them to the outbox table **inside the same UoW transaction**. The background publisher is a true adapter — the module never leaks the concept. |
| **Audit logging** | Every service manually calls `IAuditService.LogActionAsync`. | The module wraps every `ExecuteAsync` call in a decorator/handler that logs the action based on command metadata (reflection or a `IAuditable` interface on the command). |
| **Authorization** | `ITicketAuthorizationService` is called inline in the current `TicketModule`, but some legacy services bypass it or check different rules. | Centralized: `ExecuteAsync` and `QueryAsync` both run the authorization pipeline before the handler. No command/query handler can accidentally bypass it. |
| **Notification dispatch** | `INotificationService` is called directly in `TicketAssignmentFacade` and others. | Notifications are emitted via the observer pipeline or domain events — never directly by command handlers. |
| **Transaction boundaries / UoW** | Some services call `_unitOfWork.CommitAsync()`; others rely on EF Core SaveChanges via the interceptor. Inconsistent. | The module owns the UoW. One `CommitAsync` per command. Handlers queue work on the UoW; the module commits and dispatches events. |
| **Optimistic concurrency** | `UpdateAsync` and `TransitionStatusAsync` manually compare `ticket.TicketStatus.ToString()` against the command's expected status. | Built into the command handler for `UpdateTicketCommand` and `TransitionStatusCommand`. The rest don't need it. |
| **Status transition validation** | `Ticket.IsValidTransition` is called in `TicketAuthorizationService` and inline in lifecycle methods. | Part of the domain model (`ticket.TransitionTo` throws `DomainException` if invalid). The module catches and returns `Result.Failure`. |
| **Custom field serialization** | JSON serialization of `Dictionary<string, string>` is done inline in `TicketLifecycleService`. | The command handler does it. The module doesn't leak the JSON concern. |
| **GERDA AI processing** | `TicketCreatedGerdaHandler` is triggered via domain event after SaveChanges. | Same mechanism, but entirely internal. The module may eventually absorb the handler into its own pipeline. |
| **Batch operation coordination** | `TicketBatchService` iterates tickets, calls `ITicketWorkflowService`, and manually rolls back on partial failure. | `BatchAssignCommand` and `BatchUpdateStatusCommand` handlers process each ticket inside the same UoW. On failure, the batch is atomic: either all succeed or none do. |
| **Customer scoping on search** | `SearchForUiAsync` manually checks `user.IsInRole(Constants.RoleCustomer)` and injects `CustomerId` into the filter. | `QueryAsync` inspects `ClaimsPrincipal` roles and automatically injects `CustomerId` into `SearchTicketsQuery` and `SearchTicketsForUiQuery` when the caller is a customer. |
| **AI summary authorization** | `GenerateAiSummaryAsync` manually loads the ticket and calls `_auth.CanView`. | `QueryAsync` runs authorization before dispatching to the AI summary handler. |
| **Time logging / billable tracking** | `TicketTimeLoggingService` exists as a separate service with its own repository call pattern. | Absorbed into `LogTimeCommand` handler inside the module. |
| **Resolution + integration event** | `TicketResolutionService` calls `IRabbitMqPublisher` directly after commit. | Queues a `TicketResolvedEvent` to the outbox table, letting the background publisher handle retries. |

---

## 5. Dependency Strategy

The module hides dependencies by category. Only the module implementation holds references to these; controllers hold only `ITicketModule`.

### Category 1: In-process (always present, same AppDomain)

| Dependency | Ownership | Notes |
|---|---|---|
| `ITicketRepository`, `IUnitOfWork`, `IUserRepository`, `IProjectRepository` | Scoped per-request | The module implementation injects these. They are standard EF Core repositories. |
| `ITicketAuthorizationService` | Internal to module | Becomes a private authorization pipeline step, not a public service. |
| `ITicketLifecycleService` + internal handlers | Internal to module | Refactored into private command handlers. The `ITicketLifecycleService` interface can be deleted once migration is complete. |
| `ISystemClock` | Injected into module | Used for deterministic time in tests. |

### Category 2: Local-substitutable (swap via DI for testing / different deployment)

| Dependency | Ownership | Notes |
|---|---|---|
| `IOpenAiService` | Injected into module | AI summary generation. Swappable with a stub or local LLM adapter. |
| `IDispatchingService` | Injected into module | GERDA agent recommendation engine. Can be disabled or mocked. |
| `ISavedFilterService` | Injected into module | Per-user saved search filters. |
| `ITicketContextFacade` | Injected into module | UI context building (lists, configs). May be refactored into query handlers. |
| `ITicketReadService` | Internalized | Becomes query handler implementations inside the module. The public interface is deleted. |

### Category 3: Ports & Adapters (abstracted interfaces, multiple possible implementations)

| Dependency | Ownership | Notes |
|---|---|---|
| `IAuditService` | Port | The module writes audit records. The adapter could be EF Core, a separate audit DB, or an event stream. |
| `INotificationService` | Port | The module emits notifications via observers/domain events. The adapter sends email, SMS, push, or in-app. |
| `IRabbitMqPublisher` | Port | Only the outbox publisher talks to RabbitMQ. The module writes to the outbox table (SQLite/EF Core); the adapter drains it. |
| `ITenantContext` | Port | Multi-tenant resolution for integration event metadata. |

### Category 4: True external (outside process, network boundary, may be down)

| Dependency | Resilience Strategy |
|---|---|
| **RabbitMQ** | Outbox pattern + retry with exponential backoff + dead-letter after max retries. The module never blocks on RabbitMQ. |
| **OpenAI / LLM API** | Circuit breaker + fallback to cached summary or empty string. `GenerateAiSummaryQuery` can return `"Summary unavailable"` instead of failing the HTTP request. |
| **GERDA dispatching model** | `IDispatchingService.IsEnabled` flag. If the model service is down, recommendations are omitted from the dispatch backlog view. |

---

## 6. Internal Architecture (How 2 Methods Handle 25+ Operations)

```
Controller ──► ITicketModule
                  │
    ┌─────────────┴─────────────┐
    ▼                           ▼
ExecuteAsync<TResult>      QueryAsync<TResult>
    │                           │
    ▼                           ▼
Authorization Pipeline     Authorization Pipeline
    │                           │
    ▼                           ▼
Command Handler Dispatch   Query Handler Dispatch
(frozen Dictionary<Type,     (frozen Dictionary<Type,
 Delegate>)                   Delegate>)
    │                           │
    ▼                           ▼
UnitOfWork.CommitAsync()   Read-only projection
    │                           │
    ▼                           ▼
DomainEventDispatch()      Return TResult
    │
    ▼
Outbox enqueue
    │
    ▼
Observer iteration (fire-and-forget, logged swallow)
```

### Dispatch implementation (C# 13)

```csharp
internal sealed class TicketModule : ITicketModule
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<TicketModule> _logger;

    // Frozen at startup for zero-allocation dispatch
    private static readonly FrozenDictionary<Type, Delegate> _commandHandlers =
        new Dictionary<Type, Delegate>
        {
            [typeof(CreateTicketCommand)] = (Func<CreateTicketCommand, ClaimsPrincipal, CancellationToken, Task<Result<Guid>>>)HandleCreateAsync,
            [typeof(AddCommentCommand)] = (Func<AddCommentCommand, ClaimsPrincipal, CancellationToken, Task<Result<Unit>>>)HandleCommentAsync,
            // ... etc
        }.ToFrozenDictionary();

    public async Task<Result<TResult>> ExecuteAsync<TResult>(
        ITicketCommand<TResult> command,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        // 1. Authorize (generic pipeline)
        if (!await AuthorizeAsync(command, user, ct))
            return Result.Failure<TResult>("Not authorized.");

        // 2. Dispatch to typed handler
        var handler = _commandHandlers[command.GetType()];
        var result = await ((Func<object, ClaimsPrincipal, CancellationToken, Task<Result<TResult>>>)handler)
            (command, user, ct);

        // 3. Audit (generic, reflection-based or source-generated)
        await _audit.LogAsync(command, user.Identity?.Name, result.IsSuccess);

        return result;
    }
}
```

> **Note:** In practice, a cleaner dispatch mechanism is a `ITicketCommandHandler<TCommand, TResult>` interface resolved from `IServiceProvider` per command type. The frozen dictionary holds `Func<IServiceProvider, object, ClaimsPrincipal, CancellationToken, Task<object>>` wrappers that resolve the typed handler and invoke it. This avoids giant switch expressions and keeps handlers testable in isolation.

---

## 7. Trade-offs

| What you lose | Mitigation |
|---|---|
| **Granular discoverability in the interface** | The interface has 2 methods, but IntelliSense on `ITicketCommand<>` and `ITicketQuery<>` implementations shows all operations. Generate a `TicketOperations.md` or use Roslyn source generators to emit a static catalog if needed. |
| **Per-operation authorization attributes** | `[Authorize(Roles = "Admin")]` on controller actions is replaced by internal authorization checks. You can still keep `[Authorize]` at the controller level for authentication. Add a `RequiredRoles` property to commands if you want declarative role metadata for documentation. |
| **Unit-testability of the controller at method granularity** | Controllers are already thin; they become thinner. Test the command/query handlers directly. The controller tests become trivial "does it call `ExecuteAsync` with the right command?" tests. |
| **Different caching strategies per query** | Add an `ICachePolicy` marker interface to queries that need caching. The `QueryAsync` dispatcher checks for it. Or handle caching in the query handler itself — the module doesn't need to know. |
| **Single point of failure** | If `ExecuteAsync` has a bug, all writes break. Mitigate by keeping the dispatch pipeline extremely thin (authorization → handler → commit → events). All real logic lives in private handlers. The pipeline itself is ~20 lines. |
| **Loss of compile-time exhaustiveness for operations** | C# doesn't have native discriminated unions. The frozen dictionary / handler resolution is runtime. Use a unit test that scans the assembly for all `ITicketCommand<>` implementations and verifies a handler exists. |
| **Slightly harder to debug stack traces** | The generic dispatch adds one frame. Name handlers clearly (`HandleCreateTicketCommandAsync`) so the stack trace is readable. Use `ActivitySource` / OpenTelemetry inside the module for tracing. |
| **Over-generic signature intimidates junior devs** | Provide a static helper class `TicketOperations` with factory methods if discoverability is a concern: `TicketOperations.Create(cmd, user)` calls `module.ExecuteAsync(cmd, user)`. |

---

## 8. Migration Path from Current State

1. **Keep existing `ITicketModule` and `TicketModule` as-is** during migration.
2. **Add the new marker interfaces** `ITicketCommand<TResult>` and `ITicketQuery<TResult>`.
3. **Refactor existing command records** (`CreateTicketCommand`, `UpdateTicketCommand`, etc.) to implement `ITicketCommand<TResult>`.
4. **Create new command records** for the legacy GERDA operations (`AddCommentCommand`, `ResolveTicketCommand`, etc.).
5. **Implement private handlers** inside `TicketModule` for each command/query. Start with commands not yet in the module.
6. **Deprecate old interface methods** with `[Obsolete]` as their command equivalents come online.
7. **Delete the 15 legacy GERDA service interfaces** once all their operations are handled internally. Keep the implementations (or absorb their logic into private handlers).
8. **Final step:** The public interface is truly 2 methods. Delete the old 13-method `ITicketModule` interface and rename the new one if necessary.

---

## 9. Summary

- **Public surface:** 2 generic methods (`ExecuteAsync`, `QueryAsync`).
- **Real API surface:** ~15 command records + ~10 query records, all discoverable via type system.
- **Hidden complexity:** 15 legacy GERDA services, 9 observer implementations, reflection-based domain-event dispatch, outbox polling, audit logging, authorization, notification dispatch, transaction boundaries, batch coordination, optimistic concurrency, AI summary resilience, customer scoping.
- **Dependency model:** In-process repos stay injected; local-substitutable services (AI, GERDA) are module-internal; ports (audit, notification, RabbitMQ) are abstracted; true externals (RabbitMQ, OpenAI) are resilient via outbox + circuit breakers.
- **Trade-off:** You sacrifice interface-level discoverability for a deep, maintainable boundary. The code that matters moves from the service layer into testable private handlers behind an impenetrable facade.
