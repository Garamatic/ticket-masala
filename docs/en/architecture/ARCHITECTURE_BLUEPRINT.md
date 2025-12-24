# Architecture Blueprint: Ticket Masala

This document provides a comprehensive deep-dive into the technical architecture of Ticket Masala, from its core design philosophy to its specific data models and AI integration patterns.

---

## Design Philosophy: The Modular Monolith

Ticket Masala is designed as a **Modular Monolith** following a "Monolith First" doctrine. This approach prioritizes operational simplicity and high performance without the overhead of microservices, while maintaining strict logical boundaries between subsystems.

### Logical vs. Physical Separation
We utilize standard .NET abstractions to achieve logical separation while running in a single process:
- **In-Memory Messaging:** `System.Threading.Channels` instead of RabbitMQ.
- **Local Persistence:** SQLite (WAL Mode) with FTS5 for high-speed indexing.
- **Background Processing:** `IHostedService` instead of separate worker containers.

---

## The Universal Entity Model (UEM)

To remain domain-agnostic, Ticket Masala operates on three core abstractions. terminologies are mapped at the presentation layer via configuration.

| UEM Concept | Internal Implementation | Description |
| :--- | :--- | :--- |
| **WorkItem** | `Ticket` | The atomic unit of work. |
| **WorkContainer** | `Project` | A high-level grouping of work items. |
| **WorkHandler** | `ApplicationUser` | The entity (human or system) resolving the work. |

---

## Intelligence: The GERDA AI Engine

The **GERDA** engine (Grouping, Evaluation, Ranking, Dispatching, Anticipation) orchestrates the AI-driven workflow optimization.

### AI Processing Pipeline
1. **Ingestion:** Data enters via Gatekeeper API or SAP Sync.
2. **Classification (Local):** ML.NET classifies the work item and estimates effort points.
3. **Enrichment (Ephemeral):** OCR and PII scrubbing are performed in-memory.
4. **Ranking (WSJF):** Weighted Shortest Job First algorithm scores priority.
5. **Dispatching:** GERDA recommends the best handler based on affinity, skill, and load.

---

## Data Architecture

### Hybrid Storage Model
We use a hybrid relational and document-based storage approach in SQLite:
- **Universal Fields:** Indexed relational columns for core properties (Status, CreatedAt, DomainId).
- **JSON Fields:** A `CustomFieldsJson` column stores domain-specific data without requiring schema migrations.

### Performance Optimizations
- **WAL Mode:** Enables concurrent reads and writes.
- **FTS5:** Full-text search engine integrated for blazingly fast lookups across tickets and the knowledge base.
- **Computed Columns:** Key JSON properties are extracted into virtual columns for indexing.

---

## Security & Governance

### The Identity Layer
- **RBAC:** Fine-grained Role-Based Access Control (Admins, Team Leads, Agents, Customers).
- **Isolation:** Strict data partitioning via `TenantId` and `DomainId`.

### AI Sovereignty
- **PII Proxy:** Local scrubbing before any third-party AI service is called.
- **Budget Caps:** Hard and soft tokens caps per tenant to prevent operational shock.

---

## Deployment Strategy

Ticket Masala is optimized for **Cloud-Local** operation:
- **Docker-Ready:** Chiseled container images for minimal attack surface.
- **Single-Container:** One container contains the Web UI, API, and background AI workers.
- **Fly.io Optimized:** Ready for global distribution with local persistent volumes.

---

## Related Resources
- **[System Overview](../SYSTEM_OVERVIEW.md)**
- **[Detailed Capabilities](../capabilities/README.md)**
- **[Configuration Guide](../guides/configuration.md)**
