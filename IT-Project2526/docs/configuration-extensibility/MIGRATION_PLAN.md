# Architecture Evolution: Current → Extensible

**Document Purpose:** Gap analysis and phased migration plan from the current IT-focused architecture to the configuration-driven, domain-agnostic platform.

**Last Updated:** December 6, 2025

---

## 1. Executive Comparison

| Aspect | Current Architecture | Target Architecture |
|--------|---------------------|---------------------|
| **Philosophy** | IT Ticketing System with AI | Generic Workflow Engine with AI |
| **Domain Model** | Fixed (Ticket, Project, Employee) | Configurable (Work Item, Container, Handler) |
| **Data Schema** | Static EF Core entities | Hybrid: Static core + JSON custom fields |
| **Ticket Types** | C# Enum (`TicketType`) | YAML-defined string codes |
| **Workflow States** | C# Enum (`Status`) | YAML-defined per domain |
| **Business Rules** | Hardcoded in `TicketService.cs` | Rule Engine driven by YAML |
| **AI Strategies** | Single implementation per module | Strategy Pattern with domain injection |
| **Configuration** | `masala_config.json` (flat) | `masala_domains.yaml` (hierarchical) |
| **Multi-Domain** | Not supported | First-class citizen |

---

## 2. Detailed Gap Analysis

### 2.1 Data Model Layer

#### Current State

```
Models/
├── Ticket.cs          → Fixed fields (Description, Status, TicketType enum)
├── Project.cs         → Fixed structure
├── Employee.cs        → Fixed structure
└── Enums.cs           → Hardcoded Status, TicketType enums
```

**Limitation:** Adding a "Soil pH" field for gardening requires:

1. Modify `Ticket.cs`
2. Create EF migration
3. Redeploy application

#### Target State

```
Models/
├── Ticket.cs          → Universal fields + DomainId + CustomFieldsJson (JSON blob)
├── Project.cs         → Becomes generic "WorkContainer"
├── Employee.cs        → Becomes generic "WorkHandler"  
└── Enums.cs           → Deprecated for extensible types
```

**Benefit:** Adding "Soil pH" requires only editing `masala_domains.yaml`.

| Gap | Migration Action |
|-----|------------------|
| `TicketType` is enum | Add `WorkItemTypeCode` string field |
| No `DomainId` field | Add column with default `"IT"` |
| No `CustomFieldsJson` | Add nullable JSON column |
| `Status` is enum | Keep for now, introduce configurable states in Phase 2 |

---

### 2.2 Service Layer

#### Current State

```
Services/
├── TicketService.cs           → Monolithic (Query + Command + Rules)
├── ProjectService.cs
├── GERDA/
│   ├── GerdaService.cs        → Facade (good)
│   ├── GroupingService.cs     → Single implementation
│   ├── EstimatingService.cs   → Hardcoded category map
│   ├── RankingService.cs      → WSJF only
│   └── DispatchingService.cs  → Matrix Factorization only
```

**Limitation:** To use "Risk Score" ranking for Tax Law, you must modify `RankingService.cs`.

#### Target State

```
Services/
├── TicketService.cs           → Core CRUD only
├── Configuration/
│   ├── IDomainConfigurationService.cs    → NEW: Reads YAML
│   └── DomainConfigurationService.cs
├── Rules/
│   ├── IRuleEngineService.cs             → NEW: Executes transition rules
│   └── YamlRuleEngineService.cs
├── GERDA/
│   ├── GerdaService.cs                   → Injects strategies per domain
│   ├── Strategies/
│   │   ├── IRankingStrategy.cs           → NEW: Interface
│   │   ├── WSJFRankingStrategy.cs        → Current logic extracted
│   │   ├── RiskScoreRankingStrategy.cs   → NEW
│   │   └── SeasonalPriorityStrategy.cs   → NEW
│   ├── IDispatchingStrategy.cs           → NEW
│   └── ...
```

| Gap | Migration Action |
|-----|------------------|
| No domain config service | Create `IDomainConfigurationService` |
| No rule engine | Create `IRuleEngineService` |
| GERDA strategies are single-impl | Extract interfaces, implement Strategy Pattern |
| Hardcoded complexity map | Move to YAML config |

---

### 2.3 Configuration Layer

#### Current State

```json
// masala_config.json (flat structure)
{
  "AppInstanceName": "Ticket Masala",
  "Queues": [...],           // Queue definitions
  "GerdaAI": {               // AI toggle and params
    "ComplexityEstimation": { "CategoryComplexityMap": [...] }
  }
}
```

**Limitation:** No concept of "domains" or per-domain customization.

#### Target State

```yaml
# masala_domains.yaml (hierarchical, domain-aware)
domains:
  IT:
    entity_labels: { work_item: "Ticket" }
    work_item_types: [...]
    custom_fields: [...]
    workflow: { states: [...], transitions: {...} }
    ai_strategies: { ranking: "WSJF" }
  
  Gardening:
    entity_labels: { work_item: "Service Visit" }
    custom_fields:
      - name: soil_ph
        type: number
    ai_strategies: { ranking: "SeasonalPriority" }
```

| Gap | Migration Action |
|-----|------------------|
| JSON format only | Adopt YAML (more readable, supports anchors) |
| Flat structure | Introduce `domains` hierarchy |
| No custom fields schema | Add `custom_fields` block per domain |
| No workflow definition | Add `workflow.states` and `workflow.transitions` |

---

### 2.4 UI/Presentation Layer

#### Current State

- Hardcoded labels ("Ticket", "Project", "Agent")
- Hardcoded dropdowns (TicketType enum)
- Hardcoded form fields

#### Target State

- Dynamic labels from `domain.entity_labels`
- Dynamic dropdowns from `domain.work_item_types`
- Dynamic form fields from `domain.custom_fields`

| Gap | Migration Action |
|-----|------------------|
| Hardcoded "Ticket" label | Read from `IDomainConfigurationService` |
| Enum-based dropdowns | Populate from config |
| Fixed create/edit forms | Render custom fields dynamically |

---

### 2.5 GERDA AI Layer

#### Current State

| Module | Implementation | Domain Awareness |
|--------|---------------|------------------|
| **G** (Grouping) | Rule-based spam detection | ❌ Generic |
| **E** (Estimating) | Category lookup from config | ⚠️ Single config |
| **R** (Ranking) | WSJF formula | ❌ Hardcoded |
| **D** (Dispatching) | Matrix Factorization ML | ❌ Generic |
| **A** (Anticipation) | Time Series SSA | ❌ Generic |

#### Target State

| Module | Implementation | Domain Awareness |
|--------|---------------|------------------|
| **G** | Rule-based (configurable rules per domain) | ✅ |
| **E** | Strategy pattern → `IEstimatingStrategy` | ✅ |
| **R** | Strategy pattern → `IRankingStrategy` | ✅ |
| **D** | Strategy pattern → `IDispatchingStrategy` | ✅ |
| **A** | Domain-specific forecasting models | ✅ |

| Gap | Migration Action |
|-----|------------------|
| Single ranking algorithm | Create `IRankingStrategy`, extract WSJF |
| Single dispatching algorithm | Create `IDispatchingStrategy` |
| No prompt customization | Add `ai_prompts` per domain in YAML |

---

## 3. Phased Implementation Plan

### Phase 1: Foundation (Week 1-2)

**Goal:** Establish configuration infrastructure without breaking existing functionality.

| Task | File(s) | Effort |
|------|---------|--------|
| Create `masala_domains.yaml` with IT domain (mirrors current) | `/masala_domains.yaml` | S |
| Implement `IDomainConfigurationService` | `Services/Configuration/` | M |
| Add YAML parsing (YamlDotNet NuGet) | `Program.cs` | S |
| Add `DomainId` column to Ticket (default: "IT") | Migration | S |
| Add `WorkItemTypeCode` column to Ticket | Migration | S |
| Update `TicketController.Create` to read types from config | Controller, View | M |

**Deliverable:** IT domain works exactly as before, but types come from YAML.

---

### Phase 2: Custom Fields (Week 3-4)

**Goal:** Enable domain-specific data capture.

| Task | File(s) | Effort |
|------|---------|--------|
| Add `CustomFieldsJson` column to Ticket | Migration | S |
| Create `CustomFieldDefinition` model | `Models/Configuration/` | S |
| Implement custom field validation service | `Services/Validation/` | M |
| Create dynamic form renderer (Razor partial) | `Views/Shared/` | L |
| Update `Ticket/Create` and `Ticket/Edit` to render custom fields | Views | M |
| Add "Gardening" domain to YAML with custom fields | `masala_domains.yaml` | S |

**Deliverable:** Can create landscaping tickets with Soil pH field.

---

### Phase 3: Workflow Engine (Week 5-6)

**Goal:** Configurable state transitions and rules.

| Task | File(s) | Effort |
|------|---------|--------|
| Define `IRuleEngineService` interface | `Services/Rules/` | S |
| Implement YAML-based rule engine | `Services/Rules/` | L |
| Update `TicketService.UpdateStatus` to check rules | `TicketService.cs` | M |
| Add transition rules to YAML | `masala_domains.yaml` | S |
| Update UI to show valid next states only | Views | M |
| Add "TaxLaw" domain with complex transitions | `masala_domains.yaml` | S |

**Deliverable:** Status transitions respect YAML-defined rules.

---

### Phase 4: GERDA Strategy Pattern (Week 7-8)

**Goal:** Pluggable AI algorithms per domain.

| Task | File(s) | Effort |
|------|---------|--------|
| Extract `IRankingStrategy` interface | `Services/GERDA/Strategies/` | S |
| Refactor current WSJF to `WSJFRankingStrategy` | `Services/GERDA/Ranking/` | M |
| Implement `RiskScoreRankingStrategy` | `Services/GERDA/Ranking/` | M |
| Create `IGerdaStrategyFactory` | `Services/GERDA/` | M |
| Update `GerdaService` to use factory | `GerdaService.cs` | M |
| Add `ai_strategies` to domain YAML | `masala_domains.yaml` | S |

**Deliverable:** Tax cases use Risk Score; IT uses WSJF automatically.

---

### Phase 5: UI Localization & Branding (Week 9-10)

**Goal:** Domain-aware UI labels and theming.

| Task | File(s) | Effort |
|------|---------|--------|
| Create `IUiLocalizationService` | `Services/UI/` | M |
| Replace hardcoded "Ticket" labels with config lookups | Views | M |
| Add domain-specific icons and colors | `masala_domains.yaml`, CSS | S |
| Add domain switcher (for multi-domain deployments) | Layout, Navbar | M |

**Deliverable:** Gardening users see "Service Visits", IT sees "Tickets".

---

### Phase 6: Integration & Webhooks (Week 11-12)

**Goal:** External system connectivity.

| Task | File(s) | Effort |
|------|---------|--------|
| Define `IExternalDataConnector` interface | `Services/Integration/` | S |
| Implement webhook dispatch on state change | `Observers/` | M |
| Add `integrations` section to YAML | `masala_integrations.yaml` | S |
| Create stub connectors for testing | `Services/Integration/Stubs/` | M |

**Deliverable:** Webhooks fire when Tax Case moves to "Resolved".

---

## 4. Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Breaking existing IT workflows | Phase 1 defaults everything to "IT" domain |
| Performance (YAML parsing on every request) | Cache config in memory, reload on file change |
| Migration complexity (existing data) | Backfill scripts, feature flags |
| Over-engineering | Start minimal, expand config schema as needed |

---

## 5. Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Time to add new domain | Weeks (code changes) | Hours (YAML only) |
| Domains supported | 1 (IT) | 3+ (IT, Gardening, TaxLaw) |
| Custom fields per domain | 0 | Unlimited |
| AI strategy swappability | None | Per domain |

---

## 6. Appendix: File Comparison

### Before (Current)

```
IT-Project2526/
├── masala_config.json          # Flat, limited
├── Models/
│   ├── Ticket.cs               # Fixed fields
│   └── Enums.cs                # Hardcoded enums
├── Services/
│   ├── TicketService.cs        # Monolithic
│   └── GERDA/
│       └── RankingService.cs   # Single algorithm
```

### After (Target)

```
IT-Project2526/
├── masala_domains.yaml         # Hierarchical, per domain
├── masala_integrations.yaml    # External systems
├── Models/
│   ├── Ticket.cs               # + DomainId, CustomFieldsJson
│   └── Configuration/
│       ├── DomainConfig.cs
│       ├── WorkItemTypeDefinition.cs
│       └── CustomFieldDefinition.cs
├── Services/
│   ├── TicketService.cs        # Core CRUD only
│   ├── Configuration/
│   │   └── DomainConfigurationService.cs
│   ├── Rules/
│   │   └── YamlRuleEngineService.cs
│   └── GERDA/
│       ├── GerdaService.cs     # Strategy factory injection
│       └── Strategies/
│           ├── IRankingStrategy.cs
│           ├── WSJFRankingStrategy.cs
│           └── RiskScoreRankingStrategy.cs
```

---

*This document serves as the roadmap for the Configuration & Extensibility initiative.*
