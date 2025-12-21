# Controllers Referentie

Documentatie voor MVC- en API-controllers in Ticket Masala.

## Controller Architectuur

```
Controllers/
├── Api/                        # REST API-controllers
│   ├── TicketsApiController    # /api/v1/tickets
│   ├── ProjectsApiController   # /api/v1/projects
│   └── V1/                     # Versiebeheerde UEM-endpoints
│       ├── WorkItemsController    # /api/v1/work-items
│       └── WorkContainersController  # /api/v1/work-containers
├── TicketController.cs         # Hoofd-ticket MVC (partial)
├── TicketController.Filter.cs  # Filterbeheer
├── TicketController.Workflow.cs # Reacties, beoordelingen
├── TicketController.Batch.cs   # Batch-bewerkingen
├── ProjectsController.cs       # Projectbeheer
├── HomeController.cs           # Dashboard
└── ManagerController.cs        # Beheerdersweergaven
```

---

## Partial Class Patroon

De `TicketController` is opgesplitst in 'partial' klassen voor beter onderhoud:

| Bestand | Verantwoordelijkheid |
|---------|----------------------|
| `TicketController.cs` | Kern CRUD-bewerkingen |
| `TicketController.Filter.cs` | Opgeslagen filters, zoeken |
| `TicketController.Workflow.cs` | Reacties, kwaliteitsbeoordelingen, toewijzing |
| `TicketController.Batch.cs` | Bulk-bewerkingen, export, tijdsregistratie |

**Voordelen:**
- Kleinere, gefocuste bestanden (~200-400 regels elk)
- Duidelijke scheiding van verantwoordelijkheden
- Eenvoudiger door de code navigeren

---

## MVC Controllers

### TicketController

**Basisroute:** `/Ticket`  
**Autorisatie:** `[Authorize]`

| Actie | Methode | Route | Beschrijving |
|-------|---------|-------|--------------|
| Index | GET | `/Ticket` | Lijst van alle tickets |
| Detail | GET | `/Ticket/Detail/{id}` | Eén ticket bekijken |
| Create | GET/POST | `/Ticket/Create` | Nieuw ticket aanmaken |
| Edit | GET/POST | `/Ticket/Edit/{id}` | Ticket wijzigen |
| Delete | POST | `/Ticket/Delete/{id}` | Ticket verwijderen |

**Constructor Afhankelijkheden:**
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

**Basisroute:** `/Projects`  
**Autorisatie:** `[Authorize]`

| Actie | Methode | Route | Beschrijving |
|-------|---------|-------|--------------|
| Index | GET | `/Projects` | Lijst van alle projecten |
| Details | GET | `/Projects/Details/{id}` | Project bekijken |
| NewProject | GET/POST | `/Projects/NewProject` | Project aanmaken |
| Edit | GET/POST | `/Projects/Edit/{id}` | Project wijzigen |
| CreateFromTicket | GET/POST | `/Projects/CreateFromTicket/{ticketId}` | Ticket naar project converteren |
| Delete | POST | `/Projects/Delete/{id}` | Project verwijderen (Admin) |

---

### ManagerController

**Basisroute:** `/Manager`  
**Autorisatie:** `[Authorize(Roles = "Admin")]`

| Actie | Methode | Route | Beschrijving |
|-------|---------|-------|--------------|
| Index | GET | `/Manager` | Admin-dashboard |
| UserManagement | GET | `/Manager/UserManagement` | Gebruikersbeheer |
| SystemSettings | GET | `/Manager/SystemSettings` | Configuratie |
| AuditLog | GET | `/Manager/AuditLog` | Systeemlogs bekijken |

---

### HomeController

**Basisroute:** `/`  
**Autorisatie:** Gemengd

| Actie | Methode | Route | Autorisatie |
|-------|---------|-------|-------------|
| Index | GET | `/` | Anoniem |
| Dashboard | GET | `/Home/Dashboard` | Geautoriseerd |
| Privacy | GET | `/Home/Privacy` | Anoniem |
| Error | GET | `/Home/Error` | Anoniem |

---

## API Controllers

### TicketsApiController

**Basisroute:** `/api/v1/tickets` en `/api/v1/workitems`

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tickets")]
[Route("api/v{version:apiVersion}/workitems")]
public class TicketsApiController : ControllerBase
```

| Endpoint | Methode | Autorisatie | Beschrijving |
|----------|---------|-------------|--------------|
| `/external` | POST | Anoniem | Externe ticket-indiening |
| `/` | GET | Geautoriseerd | Lijst van alle tickets |
| `/{id}` | GET | Geautoriseerd | Ticket ophalen via ID |
| `/create` | POST | Geautoriseerd | Werkitem aanmaken |

---

### WorkItemsController (V1)

**Basisroute:** `/api/v1/work-items`

Volledige CRUD voor het Universal Entity Model:

| Endpoint | Methode | Beschrijving |
|----------|---------|--------------|
| `/` | GET | Lijst van alle werkitems |
| `/{id}` | GET | Ophalen via ID |
| `/` | POST | Werkitem aanmaken |
| `/{id}` | PUT | Werkitem bijwerken |
| `/{id}` | DELETE | Werkitem verwijderen |

---

### ProjectsApiController

**Basisroute:** `/api/v1/projects`

| Endpoint | Methode | Beschrijving |
|----------|---------|--------------|
| `/` | GET | Lijst van alle projecten |
| `/{id}` | GET | Project ophalen via ID |
| `/customer/{customerId}` | GET | Projecten van klant ophalen |
| `/search?query=` | GET | Projecten zoeken |
| `/statistics/{customerId}` | GET | Klantstatistieken |
| `/` | POST | Project aanmaken |
| `/generate-roadmap` | POST | AI-roadmap generering |
| `/{id}/status` | PATCH | Status bijwerken |
| `/{id}/assign-manager` | PATCH | PM toewijzen |
| `/{id}` | DELETE | Project verwijderen (Admin) |

---

## Gemeenschappelijke Patronen

### API Responswrapper

Alle API-antwoorden gebruiken een standaard wrapper:

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

### Foutafhandeling

```csharp
try
{
    var result = await _service.DoSomethingAsync();
    return Ok(ApiResponse<T>.SuccessResponse(result));
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operatie mislukt");
    return StatusCode(500, ApiResponse<T>.ErrorResponse("Er is een fout opgetreden"));
}
```

### Modelvalidatie

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

## Autorisatie

### Op rollen gebaseerde toegang

```csharp
// Alleen Admin
[Authorize(Roles = Constants.RoleAdmin)]

// Meerdere rollen
[Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]

// Elke geauthenticeerde gebruiker
[Authorize]
```

### Beschikbare Rollen

| Rol | Constante | Beschrijving |
|-----|-----------|--------------|
| Admin | `Constants.RoleAdmin` | Volledige systeemtoegang |
| Medewerker | `Constants.RoleEmployee` | Personeelslid |
| Klant | `Constants.RoleCustomer` | Externe gebruiker |

---

## API Versiebeheer

API's zijn versiebeheerd via de URL-pad:

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tickets")]
```

Toegang tot versies via:
- `/api/v1/tickets` (huidig)
- `/api/v2/tickets` (toekomstig)

---

## Verdere Informatie

- [API Referentie](../api/API_REFERENCE.md) - Volledige API-documentatie
- [Domeinmodel](DOMAIN_MODEL.md) - Entiteitsdefinities
- [Architectuuroverzicht](SUMMARY.md) - Systeemontwerp
