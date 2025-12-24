# Functional Requirements Document (FRD)

**Project:** Ticket Masala  
**Version:** 1.1 (Harmonized)

---

## 1. Ticket Lifecycle & Management

### 1.1 Ingestion & Creation
- **Multi-Channel:** Tickets must be accepted via Web UI, REST API (Gatekeeper), and SAP Snapshot Sync.
- **Validation:** Every ticket must contain a `Title`, `Description`, `DomainId`, and `TenantId`.

### 1.2 State Machine
The system enforces a strict but domain-configurable state machine:
- **Core States:** `New` → `Assigned` → `InProgress` → `ReviewPending` → `Closed`.
- **Validation:** Status transitions are governed by the **Rule Compiler**. A "New" ticket cannot be "Closed" without passing the "SignedOff" or "Resolved" predicate.

### 1.3 Assignment Logic
- **Manual:** Drag-and-drop assignment to handlers.
- **AI-Recommended:** GERDA provides a ranked list of top 3 agents with "Explainability" scores.
- **Batch Operations:** Support for assigning 100+ items to a project or handler in a single transaction.

---

## 2. GERDA AI & Automation

### 2.1 Auto-Tagging & Classification
- **ML.NET:** Analyze text to predict `EntityCategory` and `Urgency`.
- **Feedback Loop:** Allow users to "Correct" AI tags, feeding back into future model training.

### 2.2 Prioritization (WSJF)
The system must calculate a **MasalaScore** for every item:
```text
Score = (BusinessValue + TimeKriticiteit) / EstimatedEffort
```

---

## 3. Security & Governance

### 3.1 Role-Based Access (RBAC)
- **Customer:** View only own items + Public KB.
- **Agent:** View assigned items + Team dashboard + Private KB.
- **Manager:** Full visibility + Project controls + Archive rights.

### 3.2 Privacy Proxy
- **Local Scrubbing:** The system MUST detect and redact PII (SSN, Email, IBAN) before sending data to external LLM endpoints.
- **Quota Management:** Enforce token/cost caps at the Tenant level.

---

## 4. Search & Discovery

### 4.1 Global Search (FTS5)
- Provide sub-second full-text search across all Tickets and Knowledge Base snippets.
- Support for #hashtags and prefix matching.

---

## Technical Constraints
- **Performance:** DB queries must average <10ms (SQLite optimized).
- **Resilience:** Background channels must handle 1,000 requests/sec during peak ingestion.
- **Concurrency:** WAL mode must be enabled to prevent "Database Locked" errors during high-volume AI processing.
