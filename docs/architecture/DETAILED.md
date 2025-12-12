# Ticket Masala: Configuration & Extensibility Architecture

**Version:** 1.0
**Last Updated:** December 6, 2025
**Status:** RFC (Request for Comments)

---

## Executive Summary

This document defines the architectural blueprint for transforming Ticket Masala from a domain-specific IT ticketing system into a **generic, configuration-driven workflow management engine**. The core philosophy is:

> **"Everything that can be domain-specific MUST be configuration-driven."**

---

## 1. Core Concepts

### 1.1 The Universal Entity Model

The system recognizes three **universal abstractions** that exist across all domains:

| Universal Concept | Domain Examples |
|-------------------|-----------------|
| **Work Item** | IT Ticket, Tax Case, Gardening Service Visit, Government Inquiry |
| **Work Container** | IT Project, Tax Portfolio, Garden Zone, Citizen File |
| **Work Handler** | IT Agent, Tax Officer, Horticulturist, Case Worker |

These abstractions are **immutable in code** but their **labels, fields, and behaviors are mutable via configuration**.

### 1.2 The "In-Process" Architecture (KISS Principle)

We replace infrastructure components with .NET abstractions to create **"Logical Separation" without "Physical Separation."**

| "Enterprise" Component | "Origins" Replacement (C#) | Why it works |
|------------------------|---------------------------|--------------|
| **RabbitMQ** | `System.Threading.Channels` | High-perf in-memory queue. |
| **Worker Service** | `IHostedService` | Runs a background thread inside same app. |
| **Redis** | `IMemoryCache` | Uses container RAM. Faster than network. |
| **PostgreSQL** | `SQLite (WAL Mode)` | "Write-Ahead Logging" allows concurrent reads/writes. |
| **Elasticsearch** | `SQLite FTS5` | Full-text search engine built into SQLite. |

**The Exit Strategy:**
Code shouldn't leak "SQLite-isms". Use EF Core abstractions (e.g., `HasComputedColumnSql`) so that upgrading to Postgres later is just a connection string and provider change.

### 1.3 The Configuration Hierarchy

```text
┌─────────────────────────────────────────────────────────────────┐
│                      DOMAIN CONFIGURATION                        │
│  (YAML/JSON: masala_domain.yaml)                                 │
│  - Entity Labels (ticket_name: "Service Visit")                  │
│  - Custom Fields (soil_ph, tax_code, os_version)                 │
│  - Workflow States & Transitions                                 │
│  - AI Strategies (Ranking, Dispatching prompts)                  │
│  - Permissions & Roles                                           │
├─────────────────────────────────────────────────────────────────┤
│                      QUEUE CONFIGURATION                         │
│  (YAML/JSON: masala_queues.yaml)                                 │
│  - SLA Defaults per Queue                                        │
│  - Urgency Multipliers                                           │
│  - Auto-Archive Rules                                            │
├─────────────────────────────────────────────────────────────────┤
│                      GERDA AI CONFIGURATION                      │
│  (YAML/JSON: masala_gerda.yaml or embedded in domain)            │
│  - G: Spam Detection Rules                                       │
│  - E: Complexity Estimation Categories                           │
│  - R: Ranking Algorithm (WSJF, Risk Score, Custom)               │
│  - D: Dispatching Strategy (ML, Rule-Based, ERP Lookup)          │
│  - A: Forecasting Parameters                                     │
├─────────────────────────────────────────────────────────────────┤
│                      INTEGRATION CONFIGURATION                   │
│  (YAML/JSON: masala_integrations.yaml)                           │
│  - External System Connectors (ERP, CRM, Tax DB)                 │
│  - Webhook Endpoints                                             │
│  - API Keys & Auth                                               │
│  - Ingestion Methods (API, CSV, Email, ERP Sync)                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3 Integration Configuration (Per Domain)

Each domain can define its own **ingestion sources** and **outbound integrations**:

| Ingestion Type | Example Domain | Description |
|----------------|----------------|-------------|
| **API Endpoint** | Landscaping | External website form POSTs to `/api/v1/tickets/external` |
| **ERP Sync** | Procurement | Pull orders from SAP every 15 minutes |
| **CSV Import** | HR | Weekly upload of employee requests |
| **Email Ingestion** | IT Support | Parse emails from <support@company.com> |
| **Webhook Push** | Government | External system pushes cases via webhook |

**Outbound Integrations:**

| Integration Type | Example |
|------------------|---------|
| **Webhook** | Notify ERP when ticket status changes |
| **API Call** | Update CRM when ticket is closed |
| **Email** | Send notification to customer |

---

## 2. Data Model Architecture

### 2.1 Hybrid Relational + JSON Model

To support dynamic custom fields without schema migrations, the `Ticket` entity adopts a **hybrid model**:

```csharp
public class Ticket : BaseModel
{
    // ═══════════════════════════════════════════
    // UNIVERSAL FIELDS (Hard-coded, Indexed)
    // ═══════════════════════════════════════════
    public required Status TicketStatus { get; set; }
    public required string Description { get; set; }
    public string? CustomerId { get; set; }
    public string? ResponsibleId { get; set; }
    public Guid? ProjectGuid { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    
    // GERDA AI Fields (Universal)
    public int EstimatedEffortPoints { get; set; }
    public double PriorityScore { get; set; }
    public string? GerdaTags { get; set; }
    
    // ═══════════════════════════════════════════
    // DOMAIN-SPECIFIC FIELDS (JSON Blob)
    // ═══════════════════════════════════════════
    [Column(TypeName = "jsonb")] // PostgreSQL or nvarchar(max) for SQL Server
    public string? CustomFieldsJson { get; set; }
    
    // ═══════════════════════════════════════════
    // DOMAIN CONTEXT
    // ═══════════════════════════════════════════
    public required string DomainId { get; set; } // e.g., "IT", "Gardening", "TaxLaw"
    public string? WorkItemTypeCode { get; set; } // e.g., "Incident", "ServiceVisit", "Audit"
}
```

### 2.2 Custom Fields Storage & Performance

Custom fields are stored as a JSON object:

```json
{
  "soil_ph": 6.5,
  "sunlight_exposure": "Partial",
  "pest_infestation_level": "Low",
  "last_watering_date": "2025-12-01"
}
```

> [!IMPORTANT]
> **Performance Requirement:** Filtering on custom fields (e.g., `soil_ph < 6.0`) must not trigger full table scans.
>
> - **PostgreSQL:** MUST use `jsonb` column type with **GIN Indexing**.
> - **SQL Server:** MUST use **Computed Columns with Indexes** for frequently queried fields.
> - **Querying:** Do not treat the JSON column as a simple string blob; use native JSON query operators.

### 2.3 Custom Fields Validation

A new service validates the JSON against the domain configuration schema at runtime:

```csharp
public interface ICustomFieldValidationService
{
    ValidationResult Validate(string domainId, string customFieldsJson);
}
```

---

## 3. Service Architecture

### 3.1 Domain Configuration Service

The **heart of extensibility**. Reads and caches domain configurations.

```csharp
public interface IDomainConfigurationService
{
    DomainConfig GetDomain(string domainId);
    IEnumerable<WorkItemTypeDefinition> GetWorkItemTypes(string domainId);
    IEnumerable<WorkflowState> GetWorkflowStates(string domainId);
    IEnumerable<TransitionRule> GetTransitions(string domainId, string fromState);
    IEnumerable<CustomFieldDefinition> GetCustomFields(string domainId);
    string? GetAiPromptTemplate(string domainId, string module); // e.g., "Ranking", "Dispatching"
}
```

### 3.2 Rule Engine Service (Compiled)

Executes domain-specific business rules. To ensure performance, rules are **compiled at startup** into delegates using Expression Trees, rather than interpreted at runtime.

```csharp
public interface IRuleEngineService
{
    // Implementation uses compiled Func<WorkItem, bool> cached by Domain+State
    bool CanTransition(Ticket ticket, string targetState, ClaimsPrincipal user);
}
```

**Architecture Pattern:** Specification Pattern + Expression Trees.
**Performance:** Avoids repeated JSON parsing and string evaluation.

**Example Rule (YAML):**

```yaml
transitions:
  AwaitingQuote:
    - to: POIssued
      conditions:
        - field: quoted_price
          operator: is_not_empty
        - role: CFO
```

### 3.3 GERDA AI Strategy Factory

Dynamically injects the correct AI strategy based on domain configuration.

```csharp
### 3.3 GERDA AI Architecture

Transforms from simple Strategy selection to a **Feature Extraction Pipeline**.

1.  **Strategy Factory (`IStrategyFactory`):** Resolves the configured strategy implementation for the domain (e.g., loading `MatrixFactorization` for Dispatching in IT).
2.  **Feature Extraction (`IFeatureExtractor`):** Extracts and normalizes data (from JSON or SQL) into a vector.
    - Implemented by `DynamicFeatureExtractor`, driven by `masala_domains.yaml`.
    - Supports transformations: `min_max`, `one_hot`, `bool`.
3.  **Inference:** Strategies use the extracted features to run models (ML.NET/ONNX) or heuristic logic.

```csharp
public interface IFeatureExtractor
{
    // Maps Ticket + Config -> Float[]
    float[] ExtractFeatures(Ticket ticket, GerdaModelConfig config);
}
```

**Registered Strategies (via DI):**

| Domain Type | Service Interface | Implementation | Config Key |
|-------------|-------------------|----------------|------------|
| Ranking | `IJobRankingStrategy` | `WeightedShortestJobFirstStrategy` | `WSJF` |
| Estimating | `IEstimatingStrategy` | `CategoryBasedEstimatingStrategy` | `CategoryLookup` |
| Dispatching | `IDispatchingStrategy` | `MatrixFactorizationDispatchingStrategy` | `MatrixFactorization` |

> [!NOTE]
>
### 4.1 Master Domain Configuration

**File:** `masala_domains.yaml`

```yaml
domains:
  # ════════════════════════════════════════════════════════════
  # IT SUPPORT DOMAIN
  # ════════════════════════════════════════════════════════════
  IT:
    display_name: "IT Support"
    entity_labels:
      work_item: "Ticket"
      work_container: "Project"
      work_handler: "Agent"
    
    work_item_types:
      - code: INCIDENT
        name: "Incident"
        icon: "fa-fire"
        default_sla_days: 1
      - code: SERVICE_REQUEST
        name: "Service Request"
        icon: "fa-cogs"
        default_sla_days: 5
      - code: PROBLEM
        name: "Problem"
        icon: "fa-exclamation-triangle"
        default_sla_days: 14
    
    custom_fields:
      - name: affected_systems
        type: multi_select
        options: ["Email", "VPN", "ERP", "CRM", "Other"]
        required: false
      - name: os_version
        type: text
        required: false
    
    workflow:
      states: [New, InProgress, OnHold, Resolved, Closed]
      transitions:
        New: [InProgress, Closed]
        InProgress: [OnHold, Resolved]
        OnHold: [InProgress]
        Resolved: [Closed, InProgress] # Reopen allowed
        Closed: [] # Terminal
    
    ai_strategies:
      ranking: WSJF
      dispatching: MatrixFactorization
      estimating: CategoryLookup
    
    ai_prompts:
      summarize: "Summarize this IT ticket and suggest troubleshooting steps."
    
    # ─────────────────────────────────────────
    # INTEGRATION CONFIGURATION
    # ─────────────────────────────────────────
    integrations:
      ingestion:
        - type: email
          enabled: true
          config:
            mailbox: "support@company.com"
            protocol: IMAP
            polling_interval_minutes: 5
        - type: api
          enabled: true
          config:
            endpoint: "/api/v1/tickets/external"
            auth: api_key
      
      outbound:
        - type: webhook
          trigger: on_status_change
          config:
            url: "https://erp.company.com/tickets/update"
            method: POST
        - type: email
          trigger: on_resolved
          config:
            template: "ticket_resolved"
            to: "{{customer.email}}"
  
  # ════════════════════════════════════════════════════════════
  # LANDSCAPING DOMAIN
  # ════════════════════════════════════════════════════════════
  Gardening:
    display_name: "Landscaping Services"
    entity_labels:
      work_item: "Service Visit"
      work_container: "Garden Zone"
      work_handler: "Horticulturist"
    
    work_item_types:
      - code: QUOTE_REQUEST
        name: "Quote Request"
        icon: "fa-leaf"
        default_sla_days: 3
      - code: SCHEDULED_MAINTENANCE
        name: "Scheduled Maintenance"
        icon: "fa-calendar"
        default_sla_days: 7
      - code: PEST_CONTROL
        name: "Pest Control"
        icon: "fa-bug"
        default_sla_days: 2
    
    custom_fields:
      - name: soil_ph
        type: number
        min: 0
        max: 14
        required: false
      - name: sunlight_exposure
        type: select
        options: ["Full Sun", "Partial Shade", "Full Shade"]
        required: true
      - name: plant_species
        type: text
        required: false
      - name: pest_type
        type: text
        required_for_types: [PEST_CONTROL]
    
    workflow:
      states: [Requested, Quoted, Scheduled, InProgress, Completed, Cancelled]
      transitions:
        Requested: [Quoted, Cancelled]
        Quoted: [Scheduled, Cancelled]
        Scheduled: [InProgress, Cancelled]
        InProgress: [Completed]
        Completed: []
        Cancelled: []
    
    ai_strategies:
      ranking: SeasonalPriority
      dispatching: ZoneBased
      estimating: GardenComplexity
    
    ai_prompts:
      summarize: "Summarize this landscaping request. Note any plant health concerns."
    
    # ─────────────────────────────────────────
    # INTEGRATION CONFIGURATION
    # ─────────────────────────────────────────
    integrations:
      ingestion:
        - type: api
          enabled: true
          config:
            endpoint: "/api/v1/tickets/external"
            source_filter: "greenscape-landscaping"
            auth: api_key
      
      outbound:
        - type: webhook
          trigger: on_status_change
          config:
            url: "https://greenscape.com/api/job-status"
            method: POST
        - type: email
          trigger: on_quoted
          config:
            template: "quote_ready"
            to: "{{customer.email}}"
  # ════════════════════════════════════════════════════════════
  # GOVERNMENT / TAX LAW DOMAIN
  # ════════════════════════════════════════════════════════════
  TaxLaw:
    display_name: "Tax Case Management"
    entity_labels:
      work_item: "Case"
      work_container: "Portfolio"
      work_handler: "Case Officer"
    
    work_item_types:
      - code: DISPUTE
        name: "Tax Dispute"
        icon: "fa-gavel"
        default_sla_days: 90
      - code: REFUND
        name: "Refund Request"
        icon: "fa-money-bill"
        default_sla_days: 30
      - code: AUDIT
        name: "Audit"
        icon: "fa-search"
        default_sla_days: 180
    
    custom_fields:
      - name: tax_code_reference
        type: text
        required: true
      - name: case_value
        type: currency
        required: true
      - name: audit_status
        type: select
        options: ["Not Started", "In Review", "Escalated", "Concluded"]
        required_for_types: [AUDIT]
    
    workflow:
      states: [Filed, UnderReview, PendingDocuments, Escalated, Resolved, Closed]
      transitions:
        Filed: [UnderReview]
        UnderReview: [PendingDocuments, Escalated, Resolved]
        PendingDocuments: [UnderReview]
        Escalated: [Resolved]
        Resolved: [Closed]
        Closed: []
      
      transition_rules:
        - from: UnderReview
          to: Escalated
          conditions:
            - field: case_value
              operator: ">"
              value: 100000
            - role: SeniorOfficer
    
    ai_strategies:
      ranking: RiskScore
      dispatching: ExpertiseMatch
      estimating: LegalComplexity
    
    ai_prompts:
      summarize: "Summarize this tax case. Cite relevant IRC sections if applicable."
```

---

## 2.1 Configuration Philosophy

The system prioritizes configuration over hardcoding. YAML files define rules, weights, and behaviors, ensuring flexibility and domain independence.

### Example: Dispatch Weights

```yaml
weights:
  skill: 0.4
  availability: 0.3
  urgency: 0.3
```

---

For a detailed critique of the configuration approach, see [Challenges](architecture/design-input/03-technical-critique.md).

## 5. Implementation Phases

### Phase 1: Configuration Infrastructure (Current)

- [ ] Implement `IDomainConfigurationService` to read `masala_domains.yaml`
- [ ] Create `DomainConfig`, `WorkItemTypeDefinition`, `CustomFieldDefinition` models
- [ ] Add `DomainId` and `CustomFieldsJson` to `Ticket` entity + migration
- [ ] Update `Ticket/Create` view to use configured Work Item Types

### Phase 2: Workflow Engine

- [ ] Implement `IRuleEngineService` for state transition validation
- [ ] Update `TicketService` to enforce transition rules from config
- [ ] Add UI indicators for valid next states

### Phase 3: GERDA AI Pluggability

- [ ] Implement `IGerdaStrategyFactory`
- [ ] Create alternative ranking strategies (RiskScore, SeasonalPriority)
- [ ] Add domain-aware prompt templates to GERDA modules

### Phase 4: ERP/External Integration

- [ ] Define `IExternalDataConnector` interface
- [ ] Implement stub connectors for testing
- [ ] Add webhook dispatch on state transitions

---

## 6. Migration Strategy

To preserve existing data during the transition:

1. **Database Migration:**
   - Add `DomainId` column with default value `"IT"` (existing tickets become IT domain)
   - Add `CustomFieldsJson` column (nullable)
   - Add `WorkItemTypeCode` column

2. **Data Backfill:**
   - Map existing `TicketType` enum values to new `WorkItemTypeCode` strings
   - Optionally, migrate specific columns to `CustomFieldsJson`

3. **Code Cleanup (Later):**
   - Deprecate `TicketType` enum in favor of string `WorkItemTypeCode`

---

## 7. Security Considerations

- **Config File Access:** YAML files must be protected (not in wwwroot)
- **Custom Field Injection:** JSON input must be sanitized
- **Role Validation:** Transition rules referencing roles must validate against ASP.NET Identity

## 8. Open Questions & Recommendations

### 8.1 Hot Reload

**Question:** Should config changes require app restart?

**Recommendation:** No. Implement a caching layer with manual invalidation.

**Implementation:**

- Current `DomainConfigurationService` already has `ReloadConfiguration()` method
- Add admin-only `/api/config/reload` endpoint
- Optionally add file watcher for auto-detection of YAML changes

---

### 8.2 Multi-Tenancy

**Question:** Is domain synonymous with tenant?

**Recommendation:** No, decouple them:

- **Tenant** = Organization (Company A vs Company B)
- **Domain** = Business Process within tenant (IT vs HR)

**Implementation:**

- Add `TenantId` column to `Ticket`, `Project`, etc.
- Filter by `TenantId` in all service calls
- Consider implementing in a future phase after single-tenant is stable

> [!NOTE]
> Multi-tenancy is a significant architectural change. Recommend phasing:
>
> - Phase 1 (Current): Single-tenant, multi-domain
> - Phase 2 (Future): Full multi-tenancy

---

### 8.3 Configuration Versioning & Snapshot Strategy (CRITICAL)

**The Risk:** Changing a rule in `masala_domains.yaml` could break validation for existing tickets created under previous rules.

**The Solution:** Snapshot Strategy.

1. **DomainConfigVersion Entity:** Stores immutable snapshots of configuration.

    ```csharp
    public class DomainConfigVersion {
        public int Id { get; set; }
        public string DomainId { get; set; }
        public string ConfigJson { get; set; }
        public string VersionHash { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    ```

2. **Ticket Link:** `Ticket` entity stores `ConfigVersionId` at creation time.
3. **Rule Engine:** Requests the *specific version* of rules matching the ticket, not just the logical "Live" version.
4. **Lazy Compilation Cache:** `Dictionary<(string DomainId, string VersionHash), CompiledPolicy>`.
    - On request, if version not in cache, compile and cache on demand.

**Recommendation:** **MANDATORY** before Phase 5 (Rule Compiler).

---

*This document is subject to review and iteration.*
