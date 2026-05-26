# Caller-Optimized TicketModule Interface Design

> **Design goal:** A controller should perform 90% of its work with 1–2 method calls. The interface feels native to ASP.NET Core MVC (ClaimsPrincipal, ModelState, CancellationToken). Clarity over abstraction.

---

## 1. Interface Signature

### Philosophy
The interface is organized by **caller workflow**, not technical layer. Every write operation accepts `ClaimsPrincipal` and derives identity/roles/customer-scoping internally. Every operation accepts `CancellationToken` for HTTP request abortion. The module owns command construction, authorization checks, validation, context reloading, and error mapping.

### Supporting Types

```csharp
// ─── Unified action result — maps directly to MVC patterns ───────────────
public sealed record TicketActionResult
{
    public bool IsSuccess { get; private init; }
    public bool IsNotFound { get; private init; }
    public bool IsUnauthorized { get; private init; }
    public bool HasValidationErrors { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string? SuccessMessage { get; private init; }
    public Guid? EntityId { get; private init; }

    // Reload context for form recovery on failure (populated only for Create/Edit)
    public TicketFormContext? ReloadContext { get; private init; }
    public IReadOnlyList<(string Key, string Message)> ValidationErrors { get; private init; }
        = Array.Empty<(string, string)>();

    public static TicketActionResult Success(Guid? entityId = null, string? message = null)
        => new() { IsSuccess = true, EntityId = entityId, SuccessMessage = message };

    public static TicketActionResult NotFound(string? message = null)
        => new() { IsNotFound = true, ErrorMessage = message };

    public static TicketActionResult Unauthorized(string? message = null)
        => new() { IsUnauthorized = true, ErrorMessage = message };

    public static TicketActionResult Failure(string error, TicketFormContext? reloadContext = null)
        => new() { ErrorMessage = error, ReloadContext = reloadContext };

    public static TicketActionResult ValidationFailure(
        IEnumerable<(string Key, string Message)> errors,
        TicketFormContext? reloadContext = null)
        => new() { HasValidationErrors = true, ValidationErrors = errors.ToList(), ReloadContext = reloadContext };
}

// ─── Batch result — per-item outcomes with aggregate stats ───────────────
public sealed record BatchResult
{
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public IReadOnlyList<BatchItemError> Errors { get; init; } = Array.Empty<BatchItemError>();
    public bool AllSucceeded => FailureCount == 0;
    public string SummaryMessage => $"{SuccessCount} succeeded, {FailureCount} failed.";
}

public sealed record BatchItemError(Guid TicketId, string Error);

// ─── Form context for dropdown repopulation on failure ───────────────────
public sealed record TicketFormContext
{
    public List<SelectListItem> Employees { get; init; } = new();
    public List<SelectListItem> Projects { get; init; } = new();
    public List<SelectListItem> Customers { get; init; } = new();
    public List<SelectListItem> WorkItemTypes { get; init; } = new();
    public string DomainId { get; init; } = "IT";
    public EntityLabels EntityLabels { get; init; } = new();
    public List<CustomFieldDefinition> CustomFields { get; init; } = new();
    public Dictionary<string, JsonElement> CustomFieldValues { get; init; } = new();
    public List<string> ValidStatuses { get; init; } = new();
}
```

### The Interface

```csharp
public interface ITicketModule
{
    // ═══════════════════════════════════════════════════════════════════════
    // SEARCH & LIST  (TicketController.Index, TicketSearchController)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Executes a search with role-based customer scoping, saved filters,
    /// and select-list hydration. Returns a fully populated view model.
    /// </summary>
    Task<TicketSearchViewModel> SearchAsync(
        TicketSearchViewModel criteria,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // DETAIL  (TicketController.Detail)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the detail page view model plus domain context for polymorphic UI.
    /// Throws <see cref="UnauthorizedAccessException"/> if user cannot view.
    /// </summary>
    Task<(TicketDetailsViewModel ViewModel, TicketDetailContext Context)> GetDetailAsync(
        Guid ticketId,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // CREATE  (TicketController.Create GET/POST)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads all dropdowns and domain config for the creation form.
    /// </summary>
    Task<TicketCreateContext> GetCreateContextAsync(
        Guid? projectGuid,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a ticket from form input. Derives customer scoping from ClaimsPrincipal,
    /// extracts custom fields from the form collection, validates input, and returns
    /// a result with an optional reload context on failure.
    /// </summary>
    Task<TicketActionResult> CreateAsync(
        CreateTicketForm form,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // EDIT  (TicketController.Edit GET/POST)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads edit context including valid transitions, custom field values,
    /// and all dropdowns. Returns null if ticket not found.
    /// </summary>
    Task<TicketEditContext?> GetEditContextAsync(
        Guid ticketId,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a ticket from form input. Handles optimistic concurrency check
    /// on status transition, extracts custom fields, validates transitions,
    /// and returns a result with reload context on failure.
    /// </summary>
    Task<TicketActionResult> EditAsync(
        EditTicketForm form,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // WORKFLOW ACTIONS  (TicketWorkflowController, TicketCommentsController)
    // ═══════════════════════════════════════════════════════════════════════

    Task<TicketActionResult> AddCommentAsync(
        Guid ticketId,
        string body,
        bool isInternal,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    Task<TicketActionResult> ResolveAsync(
        Guid ticketId,
        string resolutionNotes,
        decimal? billableAmount,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    Task<TicketActionResult> AssignAsync(
        Guid ticketId,
        string agentId,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    Task<TicketActionResult> RequestReviewAsync(
        Guid ticketId,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    Task<TicketActionResult> SubmitReviewAsync(
        Guid ticketId,
        int score,
        string feedback,
        bool approved,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    Task<TicketActionResult> LogTimeAsync(
        Guid ticketId,
        double hours,
        DateTime date,
        string description,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // BATCH OPERATIONS  (TicketBatchController)
    // ═══════════════════════════════════════════════════════════════════════

    Task<BatchResult> BatchAssignAsync(
        IReadOnlyList<Guid> ticketIds,
        string agentId,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    Task<BatchResult> BatchTransitionAsync(
        IReadOnlyList<Guid> ticketIds,
        Status toStatus,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // API OPERATIONS  (TicketsApiController, WorkItemsController)
    // ═══════════════════════════════════════════════════════════════════════
    // Slim overloads for REST callers that don't need MVC form context.

    Task<TicketDetailsDto?> GetApiDetailsAsync(
        Guid ticketId,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    Task<Common.Result<Guid>> CreateFromApiAsync(
        CreateWorkItemRequest request,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    Task<Common.Result<Unit>> ResolveFromApiAsync(
        Guid ticketId,
        string resolutionNotes,
        decimal? billableAmount,
        ClaimsPrincipal user,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // AI / VALUE-ADD  (TicketController.GenerateAiSummary)
    // ═══════════════════════════════════════════════════════════════════════

    Task<string> GenerateAiSummaryAsync(
        Guid ticketId,
        ClaimsPrincipal user,
        CancellationToken ct = default);
}
```

### Form Types (Module-Owned, MVC-Binding Friendly)

```csharp
/// <summary>
/// Binds directly from MVC model binding or form collection.
/// The module extracts custom fields from HttpContext.Request.Form internally.
/// </summary>
public sealed class CreateTicketForm
{
    public string? Description { get; set; }
    public string? CustomerId { get; set; }
    public string? ResponsibleId { get; set; }
    public Guid? ProjectGuid { get; set; }
    public DateTime? CompletionTarget { get; set; }
    public string? DomainId { get; set; }
    public string? WorkItemTypeCode { get; set; }

    // Custom fields are NOT bound as properties; the module extracts them
    // from IFormCollection via the "customFields[Key]" naming convention.
}

public sealed class EditTicketForm
{
    public Guid Guid { get; set; }
    public string? Description { get; set; }
    public string? TicketStatus { get; set; }   // Raw string from form; module parses/validates
    public DateTime? CompletionTarget { get; set; }
    public string? CustomerId { get; set; }
    public Guid? ProjectGuid { get; set; }
}
```

---

## 2. Usage Examples

### `TicketController.Create()` — After Refactor

```csharp
[Authorize]
public class TicketController : Controller
{
    private readonly ITicketModule _ticketModule;

    public TicketController(ITicketModule ticketModule)
    {
        _ticketModule = ticketModule;
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? projectGuid = null)
    {
        var context = await _ticketModule.GetCreateContextAsync(
            projectGuid, User, HttpContext.RequestAborted);

        PopulateCreateViewBag(context);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketForm form)
    {
        var result = await _ticketModule.CreateAsync(
            form, User, HttpContext.RequestAborted);

        if (result.IsSuccess)
        {
            TempData["Success"] = result.SuccessMessage
                ?? "Ticket created successfully! GERDA AI has processed the ticket.";
            return RedirectToAction("Index", "TicketSearch");
        }

        if (result.HasValidationErrors)
        {
            foreach (var (key, message) in result.ValidationErrors)
                ModelState.AddModelError(key, message);
        }
        else if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            ModelState.AddModelError("", result.ErrorMessage);
        }

        // Reload context is pre-populated by the module on failure
        PopulateCreateViewBag(result.ReloadContext ?? new TicketFormContext());
        return View(form);
    }

    private void PopulateCreateViewBag(TicketCreateContext ctx)
    {
        ViewBag.Employees = ctx.Employees;
        ViewBag.Projects = ctx.Projects;
        ViewBag.Customers = ctx.Customers;
        ViewBag.PreselectedProjectId = ctx.PreselectedProjectId;
        ViewBag.PreselectedCustomerId = ctx.PreselectedCustomerId;
        ViewBag.IsCustomer = ctx.IsCustomer;
        ViewBag.DomainId = ctx.DomainId;
        ViewBag.EntityLabels = ctx.EntityLabels;
        ViewBag.WorkItemTypes = ctx.WorkItemTypes;
        ViewBag.CustomFields = ctx.CustomFields;
    }

    private void PopulateCreateViewBag(TicketFormContext ctx)
    {
        ViewBag.Employees = ctx.Employees;
        ViewBag.Projects = ctx.Projects;
        ViewBag.Customers = ctx.Customers;
        ViewBag.DomainId = ctx.DomainId;
        ViewBag.EntityLabels = ctx.EntityLabels;
        ViewBag.WorkItemTypes = ctx.WorkItemTypes;
        ViewBag.CustomFields = ctx.CustomFields;
    }
}
```

**What changed:**
- **Before:** 45 lines of manual validation, customer scoping, custom field extraction, command construction, error handling, and context reloading.
- **After:** 12 lines. The module owns command construction, custom field extraction, customer scoping, validation, and reload context hydration.

---

### `TicketWorkflowController.Resolve()` — After Refactor

```csharp
[Authorize]
public class TicketWorkflowController : Controller
{
    private readonly ITicketModule _ticketModule;

    public TicketWorkflowController(ITicketModule ticketModule)
    {
        _ticketModule = ticketModule;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(
        Guid id,
        string resolutionNotes,
        decimal? billableAmount)
    {
        var result = await _ticketModule.ResolveAsync(
            id, resolutionNotes, billableAmount, User, HttpContext.RequestAborted);

        return result.MatchAction(
            onSuccess: () =>
            {
                TempData["Success"] = "Ticket resolved successfully.";
                return RedirectToAction("Detail", "Ticket", new { id });
            },
            onNotFound: () => NotFound(),
            onUnauthorized: () => Forbid(),
            onFailure: error =>
            {
                TempData["Error"] = error;
                return RedirectToAction("Detail", "Ticket", new { id });
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, string commentBody, bool isInternal)
    {
        var result = await _ticketModule.AddCommentAsync(
            id, commentBody, isInternal, User, HttpContext.RequestAborted);

        if (result.IsSuccess)
        {
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                var detail = await _ticketModule.GetDetailAsync(id, User, HttpContext.RequestAborted);
                return PartialView("_CommentListPartial", detail.ViewModel.Comments);
            }
            TempData["Success"] = "Comment added.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to add comment.";
        }

        return RedirectToAction("Detail", "Ticket", new { id });
    }
}
```

**What changed:**
- **Before:** 6 separate service dependencies (`ITicketWorkflowService`, `ITicketReadService`, `IHttpContextAccessor`), manual userId extraction, manual exception handling, direct repository calls in some controllers.
- **After:** Single dependency `ITicketModule`. User identity derived from `ClaimsPrincipal`. No `IHttpContextAccessor`. No manual exception catching for domain errors — the module maps them to `TicketActionResult`.

---

### `TicketsApiController.ResolveTicket()` — After Refactor

```csharp
[ApiController]
public class TicketsApiController : ControllerBase
{
    private readonly ITicketModule _ticketModule;

    public TicketsApiController(ITicketModule ticketModule)
    {
        _ticketModule = ticketModule;
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> ResolveTicket(Guid id, ResolveTicketRequest request)
    {
        var result = await _ticketModule.ResolveFromApiAsync(
            id, request.ResolutionNotes, request.BillableAmount, User, HttpContext.RequestAborted);

        return result.Match(
            onSuccess: () => Ok(new
            {
                ticket_id = id.ToString(),
                status = "resolved",
                resolution_notes = request.ResolutionNotes,
                billable_amount = request.BillableAmount
            }),
            onFailure: error => BadRequest(new { error }));
    }
}
```

**What changed:**
- **Before:** Mixed service + repository + `UserManager` calls. Manual validation. Direct `ArgumentException` throws for bad input.
- **After:** Single call to module. Module handles validation and returns a typed `Result<Unit>`.

---

## 3. What Complexity It Hides Internally

The module implementation absorbs the following cross-cutting concerns so controllers never see them:

### A. Identity Derivation & Role-Based Scoping
- Extracts `userId` from `ClaimTypes.NameIdentifier`
- Extracts roles from `ClaimTypes.Role` claims
- Auto-scopes `CustomerId` to the current user when `User.IsInRole(Constants.RoleCustomer)`
- Passes the derived identity to authorization and command layers

### B. Custom Field Extraction
- Parses `HttpContext.Request.Form` for keys matching `customFields[*]`
- Builds the `Dictionary<string, string>` that domain methods expect
- Done internally via an `IHttpContextAccessor` the module owns — controllers never touch form parsing

### C. Authorization Gating
- Every write method loads the ticket, checks `ITicketAuthorizationService`, and returns `TicketActionResult.Unauthorized()` instead of throwing
- View operations throw `UnauthorizedAccessException` only when the caller explicitly needs to distinguish `Forbid()` from `NotFound()`
- No controller ever calls `CanEdit`, `CanView`, `CanAssign` directly

### D. Command Construction & Validation
- `CreateTicketForm` → `CreateTicketCommand` mapping happens inside the module
- `EditTicketForm` → `UpdateTicketCommand` mapping includes status parsing and optimistic concurrency seeding
- Domain validation (empty description, invalid status transitions) is caught and mapped to `TicketActionResult.ValidationFailure()`

### E. Context Reload on Failure
- On `CreateAsync` failure, the module internally calls `GetCreateContextAsync` with the same parameters and packages it into `result.ReloadContext`
- On `EditAsync` failure, the module calls `GetEditReloadContextAsync` internally
- Controllers no longer write defensive `?? new List<SelectListItem>` fallbacks

### F. Domain Event Dispatch & Outbox
- `TicketLifecycleService` commits via `IUnitOfWork.CommitAsync(ct)`
- `DomainEventDispatchingInterceptor` captures `IHasDomainEvents` aggregates after save
- Events dispatch via reflection-based dispatcher; some events write to SQLite outbox
- `OutboxPublisher` polls and forwards to RabbitMQ
- **Controllers know nothing about any of this.**

### G. Observer Notification
- `ITicketObserver` implementations (Gerda, Logging, Notification) are wired into `TicketCommentService`, `TicketResolutionService`, `TicketBatchService`
- After commit, observers fire `OnTicketUpdatedAsync`, `OnTicketCommentedAsync`, `OnTicketCompletedAsync`
- The module wraps each observer invocation in `try/catch` so a failing observer never rolls back the transaction

### H. GERDA Service Delegation
- The 15 legacy GERDA services are still used internally, but the module is the single facade:
  - `ITicketCreationService` → `CreateAsync`
  - `ITicketUpdateService` → `EditAsync`
  - `ITicketResolutionService` → `ResolveAsync`
  - `ITicketCommentService` → `AddCommentAsync`
  - `ITicketReviewService` → `RequestReviewAsync`, `SubmitReviewAsync`
  - `ITicketTimeLoggingService` → `LogTimeAsync`
  - `ITicketBatchService` → `BatchAssignAsync`, `BatchTransitionAsync`
- The obsolete `ITicketWorkflowService` is **removed from all controller constructors** and retired after migration.

---

## 4. Dependency Strategy Per Category

### Category A: Hidden Core — Module-Internal Only

| Interface | Role | Consumer |
|-----------|------|----------|
| `ITicketLifecycleService` | UoW-coordinated writes (create, update, assign, transition) | `TicketModule` only |
| `ITicketQueryService` | Canonical queries, search, dropdown hydration | `TicketModule` only |
| `ITicketAuthorizationService` | Role/ownership authorization rules | `TicketModule` only |
| `ITicketContextFacade` | UI context builders (create/edit/detail contexts) | `TicketModule` only |
| `ITicketReadService` | Legacy read facade (being absorbed into `ITicketQueryService`) | `TicketModule` only |
| `ISavedFilterService` | Saved search filters | `TicketSearchController` + `TicketModule` |

**Registration:** All scoped, internal visibility where possible (`internal interface`).

### Category B: Hidden Workflow — Service-Layer Internals

| Interface | Role | Consumer |
|-----------|------|----------|
| `ITicketCommentService` | Comment CRUD + audit + observer notify | `TicketModule` only |
| `ITicketResolutionService` | Resolve + RabbitMQ publish + outbox fallback | `TicketModule` only |
| `ITicketReviewService` | Quality review request/submit | `TicketModule` only |
| `ITicketTimeLoggingService` | Time entry creation | `TicketModule` only |
| `ITicketBatchService` | Batch assign + batch status | `TicketModule` only |
| `ITicketCreationService` | Domain ticket factory + persist | `TicketModule` only |
| `ITicketUpdateService` | Domain ticket mutate + persist | `TicketModule` only |
| `ITicketAssignmentFacade` | Agent + project assignment | `TicketModule` only |

**Registration:** All scoped, `internal class` implementations. These survive as implementation detail but are **invisible to controllers**.

### Category C: Hidden Infrastructure

| Component | Role | Visibility |
|-----------|------|------------|
| `IUnitOfWork` / `EfCoreUnitOfWork` | Transaction boundary | Internal services only |
| `ITicketRepository` | Aggregate persistence | Internal services only |
| `IUserRepository` | User lookups | Internal services only |
| `IAuditService` | Audit trail logging | Internal services only |
| `IRabbitMqPublisher` | Integration event publish | `TicketResolutionService` only |
| `DomainEventDispatchingInterceptor` | EF Core post-save event dispatch | EF pipeline only |
| `OutboxPublisher` (BackgroundService) | SQLite → RabbitMQ drain | Hosted service only |
| `IOpenAiService` | AI summary generation | `TicketModule` only |

### Category D: Exposed to Callers — The Surface Area

| Interface | Role | Consumers |
|-----------|------|-----------|
| `ITicketModule` | **The only interface controllers depend on** | All ticket-related controllers |
| `ISavedFilterService` | Filter CRUD (distinct enough to stay exposed) | `TicketSearchController` |

**Registration:** `AddTicketModule()` registers everything. Controllers inject only `ITicketModule`.

### Migration Path

1. **Phase 1 (Immediate):** Controllers add `ITicketModule` alongside existing services. New actions use `ITicketModule`.
2. **Phase 2 (2 sprints):** Migrate all controller actions to `ITicketModule`. Mark `ITicketWorkflowService` methods `[Obsolete(error: true)]`.
3. **Phase 3 (Final):** Remove `ITicketWorkflowService`, `ITicketReadService`, `IHttpContextAccessor` from all controller constructors. Update `TicketServiceCollectionExtensions` to stop registering obsolete facades.

---

## 5. Trade-Offs

### What You Lose by Optimizing for Common Callers

#### 1. **MVC Types in the Module Layer**
`ClaimsPrincipal`, `ModelStateDictionary`, `SelectListItem`, and `IFormCollection` leak into the module interface. The module is no longer a pure domain layer — it speaks ASP.NET Core MVC natively.

**Mitigation:** Keep a thin `TicketModule` that maps MVC types to clean internal commands immediately. The internal `ITicketLifecycleService` and `ITicketQueryService` remain MVC-free. If a non-MVC caller (console app, background worker) needs ticket operations, it uses the internal services directly.

#### 2. **Reduced Composability**
Because the module bundles authorization + validation + command construction + persistence + context reloading into one call, you cannot easily mix-and-match steps. A controller cannot "just validate without saving" or "just build the command for logging."

**Mitigation:** The internal services still exist for edge cases. The module is the **default** path, not the only path. Document that advanced scenarios can bypass the module.

#### 3. **Form Types Owned by the Module**
`CreateTicketForm` and `EditTicketForm` are presentation-layer shapes living in the module namespace. If the UI changes its binding convention (e.g., custom fields rename from `customFields[*]` to `fields[*]`), the module must change too.

**Mitigation:** Keep form types minimal — thin DTOs with no behavior. The module's internal `IFormCollection` parser is a single private method that is easy to update.

#### 4. **Harder to Unit Test in Isolation**
Testing `TicketModule.CreateAsync` requires mocking `IHttpContextAccessor` (for form extraction) and `ClaimsPrincipal` (for identity). This is more ceremony than testing a pure command handler.

**Mitigation:** Test coverage should focus on the internal services (`TicketLifecycleService`, `TicketAuthorizationService`) where the real logic lives. The module is a thin orchestrator — test it with integration tests, not heavy mocking.

#### 5. **API Controllers Get Slightly Different Overloads**
API controllers use `CreateFromApiAsync` and `ResolveFromApiAsync` instead of the form-based methods. This creates mild duplication in the module interface.

**Mitigation:** The API overloads are thin wrappers that build the same internal commands. The implementation delegates to a shared private method. The duplication is intentional to keep each caller's API ergonomic.

#### 6. **Single Large Interface**
`ITicketModule` has ~15 methods. This violates the "small interface" principle.

**Mitigation:** The interface is large but **shallow** — every method is a single orchestration step. It is organized by caller workflow, not technical layer, so navigating it is intuitive. If it grows beyond 20 methods, split into `ITicketModule` + `ITicketBatchModule` + `ITicketApiModule`.

---

## Summary

| Concern | Before (Current) | After (Proposed) |
|---------|-----------------|------------------|
| Controller dependencies | 3–6 services + `IHttpContextAccessor` | 1 interface: `ITicketModule` |
| User identity extraction | Manual in every action | Derived from `ClaimsPrincipal` by module |
| Custom field parsing | Manual in every Create/Edit action | Module owns form extraction |
| Authorization | Manual `CanEdit`/`CanView` calls | Module gates every operation |
| Error mapping | `try/catch` blocks per action | `TicketActionResult` with `.MatchAction()` |
| Context reload on failure | 10+ lines of `ViewBag` repopulation | `result.ReloadContext` pre-hydrated |
| Batch operations | Separate `ITicketBatchService` | Absorbed into `ITicketModule` |
| Comments / Reviews / Time | `ITicketWorkflowService` | Absorbed into `ITicketModule` |
| Domain events / outbox / observers | Scattered, implicit | Hidden behind module |

**The 90% case:** A controller action is 1 module call + 1 result mapping. That's the design.
