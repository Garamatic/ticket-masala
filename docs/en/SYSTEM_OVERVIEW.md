# Ticket Masala: System Capabilities & Architectural Overview

Ticket Masala is a next-generation **Modular Monolith** designed to bridge the gap between enterprise ERP systems (like SAP) and agile, AI-augmented operations. This document provides a detailed synthesis of the system's core capabilities, innovations, and operational principles.

---

## 1. Core Architectural Pillars

### Modular Monolith First
Ticket Masala follows a "Monolith First" approach for simplicity and performance, while maintaining strict logical boundaries between components.
- **Single Container Deployment:** Minimal DevOps overhead.
- **SQLite Performance Doctrine:** Leveraging Write-Ahead Logging (WAL) and FTS5 for high-speed local data operations without external database dependencies.
- **In-Process Integration:** Low-latency communication between core services and the AI engine.

### Multi-Tenant & Multi-Domain
The system supports a two-tier configuration model:
- **Tenants (Organization Level):** Complete data isolation and branding for different companies or departments.
- **Domains (Process Level):** Unique workflows (IT, HR, Landscaping) sharing the same tenant infrastructure but with distinct rules, terminologies, and AI strategies.

---

## 2. The Configuration Engine (DSL Compiler)

Innovation: **"Compile, Don't Interpret"**

Instead of slow runtime logic checks, Ticket Masala uses a **Domain-Specific Language (DSL)** based on YAML that is compiled into **C# Expression Trees** at startup.

- **Dynamic Terminology:** Change "Ticket" to "Incident," "Permit," or "Planting Request" via config.
- **Stateless Rules:** Business logic is defined as high-performance delegates (`Func<Ticket, bool>`), ensuring <1ms execution time.
- **Hot-Reload:** Configurations can be updated and reloaded without restarting the application.
- **Versioning:** Every configuration snapshot is hashed (SHA256) and tracked, ensuring that historical decisions are auditable even after rules change.

---

## 3. GERDA AI Dispatch Engine

The **G**rouping, **E**valuation, **R**anking, and **D**ispatch **A**lgorithm (GERDA) is the intelligence hub of the system.

### Key AI Components:
- **WSJF (Weighted Shortest Job First):** Prioritizes work based on business value, time criticality, and risk reduction divided by effort.
- **Affinity Routing:** Automatically matches repeat customers with the same agent to improve continuity and satisfaction.
- **Skill-Based Matching:** Uses proficiency-level requirements to ensure the right agent handles the right complexity.
- **Workload Balancing:** Actively prevents burnout by penalizing assignments to agents over 80% utilization.
- **Explainable AI:** Every dispatch suggestion includes a detailed breakdown (e.g., "+50 Affinity", "-20 Workload") so team leads understand the "why."

---

## 4. Scalable Ingestion & SAP Integration

### Gatekeeper API (High-Throughput)
A dedicated Minimal API project designed for **Event-Driven Intake**.
- **Asynchronous Pipeline:** Uses `System.Threading.Channels` to accept 100k+ webhooks/second without blocking.
- **Scriban Templating:** Powerful mapping engine to transform raw external JSON/CSV into the internal domain model.
- **Background Processing:** Decouples the "Acceptance" of data from its "Processing" and "Storage."

### SAP Snapshot Sync (On-Demand)
A "Read-Only Amplifier" strategy that eliminates "Excel Hell" while keeping the ERP as the single source of truth.
- **Immutable Snapshots:** Creates versioned states of SAP data linked to specific work items.
- **"Time Travel" Audits:** The ability to see exactly what the data looked like at the moment a dispatch decision was made.

---

## 5. Privacy & Governance Proxy

Innovation: **"The Compliance Fortress"**

Ticket Masala enables safe adoption of LLMs (like OpenAI/Azure) by localizing privacy and cost controls.

- **Local PII Scrubber:** Automatically detects and redacts sensitive data (NISS, VAT, IBAN, Email) *locally* before it ever reaches a cloud API.
- **Ephemeral AI Pipeline:** Processes documents (OCR → Summarize → Suggest) in memory; extracted binary blobs are discarded immediately to keep the database lean.
- **Budget Governance:** Hard and soft caps on API spending per user and per tenant to prevent "bill shock."
- **Audit Trail:** Comprehensive logs of every AI interaction, stored in a GDPR-compliant, scrubbed format.

---

## 6. Twitter-Style Knowledge Base

Innovation: **Atomic Self-Ranking Streams**

A lightweight replacement for traditional stale wikis, focused on friction-free contribution.

- **Atomic Snippets:** Knowledge units are the size of a tweet (50-300 words).
- **#Hashtag Organization:** No complex folder hierarchies; just tag and search.
- **MasalaRank Algorithm:** Content is ranked by:
  `Usage Count + (Expert Verification × 5) - Age Decay`
- **Implicit Feedback:** Snippets that successfully help close tickets automatically rise to the top of search results.
- **AI Context Injection:** The enrichment pipeline automatically pulls relevant KB snippets into LLM prompts for grounded, domain-specific AI suggestions.

---

## 7. Dashboards & Insights

- **Team Lead Atelier:** A command center for reviewing AI dispatch recommendations, overriding decisions, and monitoring real-time capacity.
- **Manager Dashboard:** High-level trends on SLA compliance, agent performance, and domain-specific throughput.
- **Compliance Dashboard:** Real-time visibility into AI costs, redacted PII counts, and governance status.

---

*This document synthesizes features and guides as of December 2025.*
