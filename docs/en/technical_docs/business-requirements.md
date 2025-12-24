# Business Requirements Document (BRD)

**Project:** Ticket Masala  
**Version:** 1.1 (Harmonized)  
**Status:** Approved

---

## Executive Summary

Ticket Masala is an enterprise-grade **Modular Monolith** designed to bridge the gap between high-overhead ERP systems and agile, AI-augmented operations. It provides a centralized, multi-tenant platform for service requests, project tracking, and intelligent work dispatching.

---

## Strategic Business Goals

1. **Operational Agility:** Reduce configuration turnaround time from "weeks" to "minutes" using Config-as-Code.
2. **AI-Driven Efficiency:** Automate the mundane (tagging, routing, estimating) via the GERDA Engine.
3. **Data Sovereignty:** Enable secure, air-gapped operations with local persistence and PII-safe AI integration.
4. **Universal Adaptability:** Support radically different domains (IT, Tax, HR) on a single unified core.

---

## Project Scope

### In-Scope
- Full ticket lifecycle management (New → Closed).
- Multi-tier configuration (Tenants vs. Domains).
- GERDA AI module (WSJF, Affinity, Skill Match).
- High-throughput ingestion (Gatekeeper API).
- Twitter-Style Knowledge Base.

### Out-of-Scope
- Real-time video conferencing.
- Direct financial accounting (integrates with SAP/Odoo instead).

---

## Stakeholder Matrix

| Stakeholder | Primary Need | Success Metric |
| :--- | :--- | :--- |
| **Customers** | Fast resolution & visibility | Reduced Time-to-Close |
| **Agents** | Efficient tooling & context | Reduced "Swivel-Chair" activity |
| **Managers** | Resource balancing & reporting | Burn-down accuracy |
| **Admins** | Low-friction configuration | Integration uptime |

---

## Success Metrics

- **Efficiency:** >40% reduction in manual ticket assignment.
- **Accuracy:** >90% precision in AI-suggested categories.
- **Speed:** API response times <100ms for core operations.
- **Security:** Zero PII leakage across the Ephemeral AI Pipeline.

---

> [!NOTE]
> This BRD is a living document and scales with the implementation phases.
