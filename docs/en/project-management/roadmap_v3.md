# Ticket Masala v3.1+ - Future Roadmap

**Version:** 3.1, 3.2, 4.0  
**Date:** December 2025  
**Status:** Planning

---

## Overview

This document covers improvements planned for **v3.1 and beyond**. These build on the v3.0 MVP foundation and include higher-effort features.

> [!NOTE]
> Complete [v3.0 MVP](./v3-roadmap-mvp.md) before starting these items.

---

## v3.1 - Configuration & Observability

### 1.1 Scriban Templates for Ingestion

**Problem:** Ingestion mappings require code changes.

**Solution:** `IngestionTemplateService` with Scriban templates from `appsettings.json`.

**Status:** Implemented

```json
// appsettings.json
"IngestionTemplates": {
  "landscaping": {
    "Title": "{{ source.subject | string.truncate 100 }}",
    "Description": "{{ source.body }}",
    "DomainId": "Gardening"
  }
}
```

**Effort:** Medium (2-3 sprints) | **Priority:** High

---

### 1.2 Configuration Hot Reload

**Problem:** Config changes require restart.

**Solution:** FileSystemWatcher with 500ms debounce.

**Status:** Implemented in `DomainConfigurationService.cs`

**Effort:** Low (1 sprint) | **Priority:** Medium

---

### 1.3 Schema Validation for CustomFieldsJson

**Problem:** No validation for dynamic JSON fields.

**Solution:** `CustomFieldValidationService` validates types, required fields, min/max, and select options.

**Status:** Already Implemented

**Effort:** Medium (2 sprints) | **Priority:** Medium

---

### 1.4 Prometheus Metrics Export

**Problem:** No visibility into GERDA performance.

**Solution:** Simple `/metrics` endpoint with uptime, memory, GC stats.

**Status:** Implemented in `Program.cs`

**Effort:** Low (1 sprint) | **Priority:** Medium

---

### 1.5 Health Check Dashboard

**Problem:** No visual overview of system health.

**Solution:** `/health` endpoint with JSON response format.

**Status:** Implemented in `Program.cs`

**Effort:** Low (0.5 sprint) | **Priority:** Medium

---

## v3.2 - AI & Plugin System

### 2.1 Explainability API

**Problem:** Users don't understand GERDA recommendations.

**Solution:** Return contributing factors with each recommendation.

```json
{
  "recommendation": { "recommendedAgent": "john.doe" },
  "explanation": {
    "factors": [
      { "name": "Category Match", "weight": 0.35 },
      { "name": "Workload Balance", "weight": 0.25 }
    ]
  }
}
```

**Effort:** High (3-4 sprints) | **Priority:** High

---

### 2.2 Feedback Loop for Learning

**Problem:** GERDA can't learn from user corrections.

**Solution:** Track acceptance/rejection of recommendations.

**Effort:** High (4+ sprints) | **Priority:** Medium

---

### 2.3 Plugin Architecture

**Problem:** Adding strategies requires code changes.

**Solution:** Runtime plugin loading from `/plugins` folder.

```csharp
public interface IMasalaPlugin
{
    string Name { get; }
    void RegisterServices(IServiceCollection services);
}
```

**Effort:** Medium (2-3 sprints) | **Priority:** Medium

---

### 2.4 Config Versioning UI

**Problem:** No way to view/rollback config changes.

**Solution:** Admin UI for configuration history.

**Effort:** Medium (2 sprints) | **Priority:** Low

---

### 2.5 Alerting Webhooks

**Problem:** Critical issues go unnoticed.

**Solution:** Webhook-based alerting for operational events.

**Effort:** Medium (1-2 sprints) | **Priority:** Low

---

## v4.0 - Enterprise Features

### 4.1 Event Sourcing (Optional)

> [!CAUTION]
> Very high effort. Only implement if audit requirements exceed what Structured Logging provides.

**Problem:** Need complete audit trail and replay capability.

**Solution:** Event store for core aggregates.

**Effort:** Very High (6+ sprints) | **Priority:** Future

---

### 4.2 NLP Summarization (Local LLM)

> [!CAUTION]
> Requires proof that GERDA has resource headroom. LLMs introduce unpredictable CPU/RAM usage.

**Problem:** Long ticket descriptions slow triage.

**Solution:** Local Phi-3/Llama for summarization.

**Effort:** High (3-4 sprints) | **Priority:** Future

---

## Priority Matrix

| Item | Version | Effort | Priority |
|------|---------|--------|----------|
| Scriban Templates | v3.1 | Medium | High |
| Explainability API | v3.2 | High | High |
| Config Hot Reload | v3.1 | Low | Medium |
| Schema Validation | v3.1 | Medium | Medium |
| Prometheus Metrics | v3.1 | Low | Medium |
| Health Dashboard | v3.1 | Low | Medium |
| Feedback Loop | v3.2 | High | Medium |
| Plugin Architecture | v3.2 | Medium | Medium |
| Config Versioning | v3.2 | Medium | Low |
| Alerting Webhooks | v3.2 | Medium | Low |
| Event Sourcing | v4.0 | Very High | Future |
| NLP Summarization | v4.0 | High | Future |

---

*See [v3.0 MVP Roadmap](./v3-roadmap-mvp.md) for immediate priorities.*
