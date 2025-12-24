# Configuration Extensibility - Implementation Phases

This document tracks the progress of the Configuration Extensibility project (`feature/configuration-extensibility`).

## 🟢 Phase 1: Infrastructure & Domain Configuration (Completed)

**Goal:** Establish the foundation for loading and serving domain-specific configurations.

- [x] **Domain Model:** Created `DomainConfig`, `WorkflowConfig`, `AiStrategiesConfig` models.
- [x] **Configuration Service:** Implemented `DomainConfigurationService` to load `masala_domains.yaml`.
- [x] **Data Persistence:** Added `DomainId` to `Ticket` entity.
- [x] **Global Defaults:** Defined default "IT" domain behavior.

## 🟢 Phase 2: Custom Fields & Dynamic UI (Completed)

**Goal:** Enable domains to define custom data fields that are rendered dynamically.

- [x] **Validation Service:** Created `ICustomFieldValidationService` for type safety (number, date, select).
- [x] **Dynamic Rendering:** Implemented `_CustomFieldsPartial.cshtml` for Create/Edit views.
- [x] **Persistence:** Implemented JSON storage (`CustomFieldsJson`) for custom data.
- [x] **Read-Only View:** Added `_CustomFieldsDisplayPartial.cshtml` for Ticket Details.
- [x] **Search/Indexing:** (Addressed via Architecture critique: JSONB/Computed Columns recommendation).

## 🟢 Phase 3: Workflow Engine (Completed)

**Goal:** Enforce domain-specific state transitions and business rules.

- [x] **Rule Engine:** Implemented `IRuleEngineService` for transition validation.
- [x] **Logic Enforcement:** Integrated checks in `TicketService.UpdateTicketAsync`.
- [x] **UI Filtering:** Updated Edit view to filter "Status" dropdown based on valid next states.
- [x] **Exception Handling:** Added graceful error messages for invalid transitions.

## � Phase 4: AI Strategy Extension (Completed)

**Goal:** Allow domains to configure which GERDA AI strategies are used for ranking, estimating, and dispatching.

- [x] **Strategy Factory:** create a factory to resolve `IJobRankingStrategy`, `IEstimatingStrategy`, etc., by name.
- [x] **Configuration Integration:** Update services to ask `DomainConfig` which strategy key to use (e.g., "TaxLaw" -> "RiskScoreRanking").
- [x] **Safety Checks:** Validate that configured strategies exist in the DI container at startup.
- [x] **Testing:** Verify different behaviors for different domains (e.g., IT uses `WSJF`, Gardening uses `SeasonalPriority`).

## � Phase 4.5: Configuration Versioning (Critical Prerequisite)

**Goal:** Implement "Snapshot Strategy" to prevent rule changes from breaking existing tickets.

- [x] **Data Model:** Create `DomainConfigVersion` entity.
- [x] **Versioning:** Store `ConfigVersionId` on Ticket creation.
- [x] **Rule Engine:** Update service to request rules by Version ID.

## �🟢 Phase 5: Performance Optimization (Rule Compiler) (Completed)

**Goal:** Refactor Rule Engine from Runtime Interpreter to Compiled Expression Trees to prevent performance degradation at scale.

**Guidance:**

- **Cache Key:** `Dictionary<(string DomainId, string VersionHash), CompiledPolicy>`
- **Safety Valve:** Wrap `Expression.Compile()` in try/catch; fallback to safe delegate on error.

- [x] **Rule Compiler Service:** Implement `RuleCompilerService` using `System.Linq.Expressions`.
- [x] **Startup Compilation:** Compile and cache all YAML rules on app startup/reload.
- [x] **Field Extractor:** Optimize JSON field access with `FieldExtractor` helper.

## 🟢 Phase 6: Advanced AI (Feature Extraction) (Completed)

**Goal:** Move beyond basic Strategy selection to dynamic Feature Extraction for ML models (ONNX/ML.NET).

- [x] **Feature Mapping:** Add `feature_mapping` format to `masala_domains.yaml`.
- [x] **Feature Extractor:** Implement `IFeatureExtractor` to convert Ticket/JSON to `float[]`.
- [x] **Integration:** Update Strategies to use extracted features for inference.

## � Phase 7: UI Localization & Branding (Completed)

**Goal:** Domain-aware UI labels and theming.

- [x] **UI Localization Service:** Replace hardcoded "Ticket" labels with config lookups.
- [x] **Domain Switcher:** Add domain switcher to layout for multi-domain deployments.
- [x] **Domain Styles:** Add domain-specific icons and color themes support.

## � Phase 8: Scalable Ingestion (Gatekeeper) (Completed)

**Goal:** Decouple ingestion to handle high throughput (IoT/Webhooks) and long-running syncs (ERP). Adopt **Event Driven Architecture** for intake.

**Decision:** Use **Scriban** for template rendering (Mapper).

- [x] **Gatekeeper API:** Create separate Minimal API project for accepting webhooks.
- [x] **Message Bus:** Implement simple producer/consumer (`System.Threading.Channels`).
- [x] **Digestion Worker:** Create `BackgroundService` to process queue items.
- [x] **Ingestion Mapping:** Detailed `ingestion` configuration in `masala_domains.yaml` using Scriban templates.

---
**Status Legend:**

- 🟢 Completed
- 🟡 In Progress / Next
- 🔴 Not Started
