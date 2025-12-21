# Controllers Reference

Documentation for MVC and API controllers in Ticket Masala.

## Controller Architecture

```
Controllers/
├── Api/                        # REST API controllers
│   ├── TicketsApiController    # /api/v1/tickets
│   ├── ProjectsApiController   # /api/v1/projects
│   └── V1/                     # Versioned UEM endpoints
│       ├── WorkItemsController    # /api/v1/work-items
│       └── WorkContainersController  # /api/v1/work-containers
├── TicketController.cs         # Main ticket MVC (partial)
├── TicketController.Filter.cs  # Filter management
├── TicketController.Workflow.cs # Comments, reviews
├── TicketController.Batch.cs   # Batch operations
├── ProjectsController.cs       # Project management
├── HomeController.cs           # Dashboard
└── ManagerController.cs        # Management views
```

---

## Partial Class Pattern

The `TicketController` is split into partial classes for maintainability:

| File | Responsibility |
|------|----------------|
| `TicketController.cs` | Core CRUD operations |
| `TicketController.Filter.cs` | Saved filters, search |
| `TicketController.Workflow.cs` | Comments, quality reviews, assignment |
| `TicketController.Batch.cs` | Bulk operations, export, time logging |

**Benefits:**
- Smaller, focused files (~200-400 lines each)
- Clear separation of concerns
- Easier code navigation

---

## MVC Controllers

### TicketController

**Base Route:** `/Ticket`  
**Authorization:** `[Authorize]`

| Action | Method | Route | Description |
|--------|--------|-------|-------------|
| Index | GET | `/Ticket` | List all tickets |
| Detail | GET | `/Ticket/Detail/{id}` | View single ticket |
| Create | GET/POST | `/Ticket/Create` | Create new ticket |
| Edit | GET/POST | `/Ticket/Edit/{id}` | Modify ticket |
| Delete | POST | `/Ticket/Delete/{id}` | Remove ticket |

**Constructor Dependencies:**
```csharp
public TicketController(
    ITicketService ticketService,
    IProjectService projectService,
    IHttpContextAccessor httpContextAccessor,
    IRuleEngineService ruleEngine,
    ILogger<TicketController> logger)
```

---

### ProjectsController

**Base Route:** `/Projects`  
**Authorization:** `[Authorize]`

| Action | Method | Route | Description |
|--------|--------|-------|-------------|
| Index | GET | `/Projects` | List all projects |
| Details | GET | `/Projects/Details/{id}` | View project |
| NewProject | GET/POST | `/Projects/NewProject` | Create project |
| Edit | GET/POST | `/Projects/Edit/{id}` | Modify project |
| CreateFromTicket | GET/POST | `/Projects/CreateFromTicket/{ticketId}` | Convert ticket to project |
| Delete | POST | `/Projects/Delete/{id}` | Remove project (Admin) |

---

### ManagerController

**Base Route:** `/Manager`  
**Authorization:** `[Authorize(Roles = "Admin")]`

| Action | Method | Route | Description |
|--------|--------|-------|-------------|
| Index | GET | `/Manager` | Admin dashboard |
| UserManagement | GET | `/Manager/UserManagement` | Manage users |
| SystemSettings | GET | `/Manager/SystemSettings` | Configuration |
| AuditLog | GET | `/Manager/AuditLog` | View system logs |

---

### HomeController

**Base Route:** `/`  
**Authorization:** Mixed

| Action | Method | Route | Authorization |
|--------|--------|-------|---------------|
| Index | GET | `/` | Anonymous |
| Dashboard | GET | `/Home/Dashboard` | Authorized |
| Privacy | GET | `/Home/Privacy` | Anonymous |
| Error | GET | `/Home/Error` | Anonymous |

---

## API Controllers

### TicketsApiController

**Base Route:** `/api/v1/tickets` and `/api/v1/workitems`

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tickets")]
[Route("api/v{version:apiVersion}/workitems")]
public class TicketsApiController : ControllerBase
```

| Endpoint | Method | Authorization | Description |
|----------|--------|---------------|-------------|
| `/external` | POST | Anonymous | External ticket submission |
| `/` | GET | Authorized | List all tickets |
| `/{id}` | GET | Authorized | Get ticket by ID |
| `/create` | POST | Authorized | Create work item |

---

### WorkItemsController (V1)

**Base Route:** `/api/v1/work-items`

Full CRUD for the Universal Entity Model:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | List all work items |
| `/{id}` | GET | Get by ID |
| `/` | POST | Create work item |
| `/{id}` | PUT | Update work item |
| `/{id}` | DELETE | Delete work item |

---

### ProjectsApiController

**Base Route:** `/api/v1/projects`

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | List all projects |
| `/{id}` | GET | Get project by ID |
| `/customer/{customerId}` | GET | Get customer's projects |
| `/search?query=` | GET | Search projects |
| `/statistics/{customerId}` | GET | Customer statistics |
| `/` | POST | Create project |
| `/generate-roadmap` | POST | AI roadmap generation |
| `/{id}/status` | PATCH | Update status |
| `/{id}/assign-manager` | PATCH | Assign PM |
| `/{id}` | DELETE | Delete project (Admin) |

---

## Common Patterns

### API Response Wrapper

All API responses use a standard wrapper:

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Error Handling

```csharp
try
{
    var result = await _service.DoSomethingAsync();
    return Ok(ApiResponse<T>.SuccessResponse(result));
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    return StatusCode(500, ApiResponse<T>.ErrorResponse("An error occurred"));
}
```

### Model Validation

```csharp
if (!ModelState.IsValid)
{
    var errors = ModelState.Values
        .SelectMany(v => v.Errors)
        .Select(e => e.ErrorMessage)
        .ToList();
    return BadRequest(ApiResponse<Guid>.ErrorResponse(errors));
}
```

---

## Authorization

### Role-Based Access

```csharp
// Admin only
[Authorize(Roles = Constants.RoleAdmin)]

// Multiple roles
[Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]

// Any authenticated user
[Authorize]
```

### Available Roles

| Role | Constant | Description |
|------|----------|-------------|
| Admin | `Constants.RoleAdmin` | Full system access |
| Employee | `Constants.RoleEmployee` | Staff member |
| Customer | `Constants.RoleCustomer` | External user |

---

## API Versioning

APIs are versioned using URL path versioning:

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tickets")]
```

Access versions via:
- `/api/v1/tickets` (current)
- `/api/v2/tickets` (future)

---

## Creating New Controllers

### MVC Controller

```csharp
[Authorize]
public class NewFeatureController : Controller
{
    private readonly IFeatureService _service;
    private readonly ILogger<NewFeatureController> _logger;

    public NewFeatureController(
        IFeatureService service,
        ILogger<NewFeatureController> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _service.CreateAsync(model);
        return RedirectToAction(nameof(Index));
    }
}
```

### API Controller

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class NewFeatureApiController : ControllerBase
{
    private readonly IFeatureService _service;

    public NewFeatureApiController(IFeatureService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FeatureDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(FeatureDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create(CreateFeatureRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
```

---

## Further Reading

- [API Reference](../api/API_REFERENCE.md) - Complete API documentation
- [Domain Model](DOMAIN_MODEL.md) - Entity definitions
- [Architecture Overview](SUMMARY.md) - System design
