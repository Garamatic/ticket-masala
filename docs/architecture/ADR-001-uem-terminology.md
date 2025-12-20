# ADR-001: Universal Entity Model Terminology Alignment

**Status:** Proposed  
**Date:** 2025-12-07  
**Decision Makers:** Architecture Team  

---

## Context

The Ticket Masala architecture defines a **Universal Entity Model (UEM)** with canonical terms:

- **WorkItem** (internal alias: `Ticket`)
- **WorkContainer** (internal alias: `Project`)

The question arose: Should we refactor the entire codebase to rename `Ticket` → `WorkItem` and `Project` → `WorkContainer`?

---

## Decision

**We will NOT perform a global rename of internal entity classes.**

Instead, we adopt a **layered terminology strategy**:

| Layer | Terminology | Rationale |
|-------|-------------|-----------|
| **Database Entities** | `Ticket`, `Project` | Stability, Git history preservation |
| **Repositories** | `ITicketRepository`, `IProjectRepository` | Internal implementation detail |
| **Services** | Keep current (optional future rename to `WorkItemService`) | Minimal disruption |
| **Public API DTOs** | `WorkItem*`, `WorkContainer*` | External consistency with UEM |
| **Views/UI Labels** | Domain-configurable via `masala_domains.yaml` | Already implemented! |
| **Generated Columns** | `WorkItem_*` prefix for new columns | Align with canonical model |

---

## Consequences

### Positive

- **Zero regression risk** - No find-replace bugs in GERDA Expression Trees
- **Git history preserved** - Blame/bisect remain useful
- **Dev velocity** - No multi-sprint rename effort
- **Already partially implemented** - Views use `entityLabels.WorkItem` from config

### Negative

- **Dual terminology** - Internal code says `Ticket`, external API says `WorkItem`
- **Onboarding overhead** - New developers must learn the mapping

### Mitigations

- Document the mapping clearly in `ARCHITECTURE_SUMMARY.md`
- Use consistent XML comments referencing UEM terms
- Enforce DTO naming in code review

---

## Implementation Summary

### Already Implemented ✅

- Views read labels from `masala_domains.yaml` via `entityLabels.WorkItem`
- `ExternalTicketRequest` DTO exists (to be aliased/renamed)

### To Be Implemented

1. **Create WorkItem DTOs** - Wrapper DTOs using UEM terminology
2. **Add API aliases** - New routes using `/api/v1/workitems`
3. **Update localization** - Ensure resource strings use "Work Item"
4. **Document mapping** - Add to architecture docs

---

## References

- [05-rewriting.md](../refactoring/input/05-rewriting.md) - Original discussion
- [architecture-v2.md](./architecture-v2.md) - Current architecture
- [masala_domains.yaml](../../masala_domains.yaml) - Domain configuration with `entity_labels`
