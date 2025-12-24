# Ticket Masala - Architecture Documentation

**Version:** 2.1 (Actual State)
**Date:** December 2025
**Status:** Living Document

---

## 1. Executive Summary

Ticket Masala is a **configuration-driven work management engine** built as a **Modular Monolith** with **AI Augmentation**. It embraces an **"In-Process" Architecture** to simplify operations while enabling extensibility through YAML configuration.

### Core Philosophy: "Everything is Configurable"

| Concern | Old Approach (v1) | Current Approach (v2) |
|---------|-------------------|----------------------|
| **Domain Logic** | Hardcoded C# classes | `masala_domains.yaml` configuration |
| **Data Schema** | Fixed SQL columns | Hybrid (SQL + JSON) Entity Model |
| **Business Rules** | `if/else` statements | Compiled Expression Trees |
| **AI Strategy** | Hardcoded logic | Feature Extraction Pipeline + Strategy Factory |
| **Infrastructure** | Docker Composition | **In-Process** (.NET Channels, SQLite WAL) |

---

## 2. The "In-Process" Infrastructure (KISS)

The system relies on **Logical Separation** rather than Physical Separation to reduce DevOps complexity.

| "Enterprise" Component | Replacement (C#) | Why it works |
|------------------------|-----------------|--------------
| **RabbitMQ** | `IHostedService` + in-memory queues | Background processing within the same OS process. |
| **Redis** | `IMemoryCache` | Zero-latency in-memory caching. |
| **PostgreSQL** | `SQLite (WAL Mode)` / SQL Server | Concurrent reads/writes; swappable via EF Core. |
| **Elasticsearch** | `SQLite FTS` | Native full-text search capability. |

**Exit Strategy:** The domain core is agnostic to the persistence layer. The EF Core provider can be swapped for SQL Server/Postgres when scaling beyond single-node capacity (already supported via `appsettings.json`).

---

## 3. Repository Structure (Actual State)

```text
ticket-masala/
├── src/
│   ├── TicketMasala.Web/              # Main ASP.NET Core MVC Application
│   │   ├── Areas/Identity/            # ASP.NET Core Identity scaffolded pages
│   │   ├── Controllers/               # MVC Controllers + API Controllers
│   │   │   ├── Api/                   # REST API endpoints
│   │   │   ├── TicketController.cs    # Core ticket management
│   │   │   ├── ProjectsController.cs  # Project management
│   │   │   └── ManagerController.cs   # Admin/management views
│   │   ├── Data/
│   │   │   ├── MasalaDbContext.cs     # EF Core DbContext
│   │   │   └── DbSeeder.cs            # Database seeding logic
│   │   ├── Engine/                    # Core business logic engines
│   │   │   ├── Compiler/              # Rule compilation (Expression Trees)
│   │   │   ├── GERDA/                 # AI subsystem
│   │   │   └── Ingestion/             # Ticket ingestion (CSV, Email, API)
│   │   ├── Models/                    # Domain entities
│   │   ├── Observers/                 # Observer pattern implementations
│   │   ├── Repositories/              # Repository pattern + Specifications
│   │   ├── Services/                  # Business services
│   │   │   └── Projects/              # Project management service
│   │   ├── ViewModels/                # MVC View Models
│   │   ├── Views/                     # Razor views
│   │   ├── masala_domains.yaml        # Domain configuration
│   │   └── masala_config.json         # GERDA AI configuration
│   └── TicketMasala.Tests/            # Unit & Integration tests
├── config/
│   └── masala_domains.yaml            # Shared domain configuration
├── samples/
│   └── landscaping-client/            # Demo integration client
├── how-to-deploy/                     # Deployment documentation
├── Dockerfile                         # Container build
└── docker-compose.yml                 # Local development orchestration
```

---

## 4. Layered Architecture

```text
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│    Controllers / Views / ViewModels / Areas/Identity         │
│              (Domain-Agnostic UI Components)                 │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                         │
│ Services:                                                   │
│  - TicketService (ITicketService, ITicketQueryService,      │
│                   ITicketCommandService)                     │
│  - ProjectService (IProjectService)                          │
│  - RuleEngineService (IRuleEngineService)                    │
├─────────────────────────────────────────────────────────────┤
│                      Domain Layer                            │
│ Models: Ticket, Project, ApplicationUser, ProjectTemplate,   │
│         Document, Notification, KnowledgeBaseArticle         │
├─────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                       │
│  EfCoreRepositories │ DynamicFeatureExtractor │ GERDA AI     │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. Configuration Extensibility

### 5.1 Domain Configuration (`masala_domains.yaml`)

The system supports multiple domains (IT, Gardening) with domain-specific:

- **Entity Labels:** Customize terminology (Ticket → "Service Visit", Agent → "Horticulturist")
- **Work Item Types:** Domain-specific ticket types with SLAs, icons, colors
- **Custom Fields:** Dynamic fields stored as JSON
- **Workflows:** State machines with transition rules
- **AI Strategies:** Domain-specific ranking, dispatching, estimating algorithms
- **Integrations:** Ingestion sources and outbound webhooks

```yaml
domains:
  IT:
    display_name: "IT Support"
    work_item_types:
      - code: INCIDENT
        name: "Incident"
        default_sla_days: 1
    custom_fields:
      - name: urgency
        type: select
        options: ["Low", "Medium", "High", "Critical"]
    ai_strategies:
      ranking: WSJF
      dispatching: MatrixFactorization
  
  Gardening:
    display_name: "Landscaping Services"
    ai_strategies:
      ranking: SeasonalPriority
      dispatching: ZoneBased
```

### 5.2 Hybrid Data Model

Entities use a mix of structured relational columns and flexible JSON storage:

```csharp
public class Ticket : BaseModel
{
    // ═══ RIGID COLUMNS (Indexed, Relational) ═══
    public required string DomainId { get; set; }        // "IT", "Gardening"
    public required string Status { get; set; }          // "New", "Triaged", "Done"
    public required string Title { get; set; }
    public string? ConfigVersionId { get; set; }         // Links to active config
    public string? ContentHash { get; set; }             // Duplicate detection
    
    // ═══ FLEXIBLE STORAGE (The "Masala" Model) ═══
    [Column(TypeName = "TEXT")]
    public required string CustomFieldsJson { get; set; } = "{}";
    
    // ═══ GERDA AI FIELDS ═══
    public int EstimatedEffortPoints { get; set; }
    public double PriorityScore { get; set; }
    public string? GerdaTags { get; set; }               // "AI-Dispatched,Spam-Cluster"
    public string? RecommendedProjectName { get; set; }
    
    // ═══ RELATIONSHIPS ═══
    public ApplicationUser? Customer { get; set; }
    public ApplicationUser? Responsible { get; set; }
    public Project? Project { get; set; }
    public List<TicketComment> Comments { get; set; }
    public List<Document> Attachments { get; set; }
}
```

---

## 6. GERDA AI Subsystem

**GERDA** (GovTech Extended Resource Dispatch & Anticipation) is a **pluggable AI pipeline** orchestrated by `GerdaService`.

### 6.1 GERDA Modules

| Letter | Module | Location | Technique |
|--------|--------|----------|-----------|
| **G** | Grouping | `Engine/GERDA/Grouping/` | K-Means clustering (spam/duplicate detection) |
| **E** | Estimating | `Engine/GERDA/Estimating/` | Classification (effort points) |
| **R** | Ranking | `Engine/GERDA/Ranking/` | WSJF, SeasonalPriority strategies |
| **D** | Dispatching | `Engine/GERDA/Dispatching/` | MatrixFactorization, ZoneBased strategies |
| **A** | Anticipation | `Engine/GERDA/Anticipation/` | Time Series (capacity forecasting) |

### 6.2 Strategy Factory Pattern

AI behaviors are injected dynamically based on domain configuration:

```csharp
// Strategies are resolved by name from DI container
public interface IStrategyFactory
{
    T? GetStrategy<T>(string strategyName) where T : class, IStrategy;
}
```

### 6.3 Feature Extraction Pipeline

```mermaid
graph LR
    T[Ticket] --> E[DynamicFeatureExtractor]
    C[YAML Config] --> E
    E --> F[float[] Vector]
    F --> M[ML Model / Strategy]
    M --> P[Prediction/Score]
```

The `DynamicFeatureExtractor` supports transformations:

- **min_max:** Normalize numeric values to [0,1]
- **one_hot:** Categorical encoding
- **bool:** Boolean to float conversion

### 6.4 Background Processing

`GerdaBackgroundService` (IHostedService) runs periodic maintenance:

- Batch processing of open tickets
- Capacity risk analysis
- Re-ranking priority scores

---

## 7. Rule Engine (Expression Trees)

The `RuleCompilerService` compiles workflow transition rules into executable delegates:

```csharp
// Config: conditions: [{field: "urgency", operator: "==", value: "Critical"}]
Func<Ticket, ClaimsPrincipal, bool> compiledRule = _ruleCompiler.Compile(conditions);

// Runtime evaluation - no string parsing
bool canTransition = compiledRule(ticket, currentUser);
```

Supported operations:

- **Role checks:** `user.IsInRole("Manager")`
- **Field comparisons:** `>`, `>=`, `<`, `<=`, `==`, `!=`
- **String matching:** Case-insensitive equality
- **Empty checks:** `is_empty`, `is_not_empty`

---

## 8. Service Architecture & Patterns

| Pattern | Implementation | Location |
|---------|----------------|----------|
| **Observer** | Ticket/Project lifecycle hooks | `Observers/` |
| **Strategy** | Pluggable AI algorithms | `Engine/GERDA/Strategies/` |
| **Factory** | Strategy resolution by config name | `StrategyFactory` |
| **Specification** | Encapsulated query logic | `Repositories/Specifications/` |
| **Repository + UoW** | Data access abstraction | `Repositories/` |
| **CQRS-lite** | Separate query/command interfaces | `ITicketQueryService`, `ITicketCommandService` |

### Key Service Interfaces

```csharp
public interface ITicketService : ITicketQueryService, ITicketCommandService, ITicketFactory
{
    // Unified interface for backward compatibility
}

public interface IProjectService
{
    Task<Project> CreateProjectAsync(Project project);
    Task<Project> CreateProjectFromTemplateAsync(Guid templateId, string name, string? managerId);
    Task<Project?> GetProjectByIdAsync(Guid id);
}
```

---

## 9. Ingestion Pipeline

### 9.1 Ingestion Sources

| Source | Implementation | Status |
|--------|----------------|--------|
| **CSV Import** | `CsvImportService` | Active |
| **Email** | `EmailIngestionService` | Active |
| **External API** | `Api/TicketsController` | Active |
| **Background Generator** | `TicketGeneratorService` | Active (demo/testing) |

### 9.2 Validation Pipeline

```text
External Input → TicketGenerator → Validation → Repository → Observer → GERDA
```

---

## 10. Security & Identity

- **Authentication:** ASP.NET Core Identity with scaffold UI
- **Authorization:** Role-based policies (Admin, Manager, Employee, Customer)
- **Tenancy:** Single-tenant, Multi-domain architecture
- **Data Privacy:** All ML processing is **local** (no external API calls)

---

## 11. Observers (Event-Driven)

Observers react to entity lifecycle events without coupling to UI:

| Observer | Trigger | Action |
|----------|---------|--------|
| `GerdaTicketObserver` | Ticket created | Process through GERDA pipeline |
| `LoggingTicketObserver` | Ticket changes | Audit logging |
| `NotificationTicketObserver` | Status changes | Send notifications |
| `NotificationProjectObserver` | Project updates | Notify stakeholders |

---

## 12. Development Status

### Completed (v2 Baseline)

- [x] Configuration Engine (YAML → Objects)
- [x] Hybrid Data Model (JSON Custom Fields)
- [x] Rule Compiler (Expression Trees)
- [x] Feature Extraction Pipeline
- [x] GERDA Core Services (G+E+R+D+A)
- [x] Observer Pattern for lifecycle hooks
- [x] Repository + Unit of Work pattern
- [x] Project Templates system
- [x] Multi-domain support (IT, Gardening)
- [x] External API ingestion
- [x] Background job processing

### 🔄 In Progress

- [ ] Strategy registration for all domain-referenced strategies
- [ ] Complete integration test coverage
- [ ] UI localization (Domain-aware labels)

---

## 13. Suggested Improvements for v3

### 13.1 Configuration & Extensibility

| Improvement | Description | Priority |
|-------------|-------------|----------|
| **Scriban Templates** | Use Scriban templating for ingestion field mapping | High |
| **Config Versioning UI** | Admin interface to view/rollback config versions | Medium |
| **Hot Reload** | Reload YAML config without app restart | Medium |
| **Schema Validation** | JSON Schema validation for `CustomFieldsJson` | Medium |

### 13.2 AI/ML Enhancements

| Improvement | Description | Priority |
|-------------|-------------|----------|
| **ML.NET Model Persistence** | Save trained models to disk for faster startup | High |
| **Explainability API** | Expose why GERDA made specific recommendations | High |
| **Feedback Loop** | Learn from user acceptance/rejection of recommendations | Medium |
| **NLP Summarization** | Auto-summarize ticket descriptions using local LLM | Low |

### 13.3 Architecture Improvements

| Improvement | Description | Priority |
|-------------|-------------|----------|
| **Event Sourcing (Optional)** | Track all state changes for audit/replay | Medium |
| **CQRS Full Implementation** | Separate read/write models for complex queries | Low |
| **Plugin Architecture** | Allow external assemblies to register strategies | Medium |
| **API Gateway Pattern** | Centralized API versioning and rate limiting | Low |

### 13.4 Developer Experience

| Improvement | Description | Priority |
|-------------|-------------|----------|
| **OpenAPI/Swagger** | Full API documentation with examples | High |
| **Integration Test Fixtures** | Reusable test data builders | High |
| **Dev Container** | Pre-configured VS Code devcontainer | Medium |
| **Performance Dashboard** | Real-time metrics for GERDA processing | Low |

### 13.5 Operations & Monitoring

| Improvement | Description | Priority |
|-------------|-------------|----------|
| **Structured Logging** | Serilog with correlation IDs | High |
| **Health Check Dashboard** | Visual health status page | Medium |
| **Prometheus Metrics** | Export metrics for Grafana dashboards | Medium |
| **Alerting Webhooks** | Notify on GERDA capacity risks | Medium |

---

## 14. Quick Start for Developers

1. **`Program.cs`** → DI setup, service registration
2. **`masala_domains.yaml`** → Domain configuration
3. **`masala_config.json`** → GERDA AI settings
4. **`DbSeeder.cs`** → Sample data initialization
5. **`TicketService.cs`** → Core business logic
6. **`GerdaService.cs`** → AI orchestration hub

---

## 15. Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Local ML.NET | GDPR privacy, no API costs, offline capability |
| Modular Monolith | Simpler ops than microservices, easy to split later |
| Observer Pattern | AI processing doesn't block UI thread |
| Repository Pattern | Testable, database-agnostic |
| Expression Trees | High-performance rule evaluation |
| Hybrid JSON Model | Schema flexibility without migrations |

---

*For detailed documentation on specific subsystems, see the `/docs` subdirectories.*
