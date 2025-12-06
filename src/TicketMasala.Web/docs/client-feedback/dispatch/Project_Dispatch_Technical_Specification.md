# 🦅 Project Dispatch: Intelligent Triage System

**French Title:** *Triage intelligent*  
**Former Name:** GERDA (Grouping Estimating Ranking Dispatching Anticipating)  
**Version:** 3.0 (Final)  
**Date:** December 4, 2025

---

**Metadata:**
- **Created:** 2025-12-02
- **Updated:** 2025-12-04
- **Tags:** project, govtech, operations, ai, dispatch, project-atom
- **Owner:** Juan Benjumea
- **Status:** Pilot
- **Priority:** High
- **Connected Projects:** Project Douane, Project Atelier (formerly HORTA), Project Codex

---

> [!abstract] Executive Summary
> **De "Logistieke Motor":** Dispatch is the intelligent operational overlay on top of legacy systems.
> * **French Concept:** *Distribution équitable du travail basée sur la complexité du dossier et la capacité réelle des agents*
> * **Metaphor:** SAP is the "Warehouse" (where things are stored). Dispatch is the "Control Tower" (that determines what goes where and when).
> * **Goal:** Create a "Digital Twin" of workload to steer teams in real-time.
> * **Motto:** "Stop Cherry-picking. Start Flow."
> * **Project Atom Context:** Phase 3 of industrialization (Valider le workflow).

---

## 1. Strategic Necessity (Why)

Large organizations (like Finance departments) rely on robust but cumbersome **Systems of Record** (e.g., SAP).

### The Problem
* SAP is the legal source of truth, but terrible for daily work management
* It's slow, non-transparent, and offers no insight into team velocity
* Team leads work blind - they don't know who's overloaded, which cases are "burning," or what's coming next week

### The Solution
**Project Dispatch** builds an **"Operational Overlay"**. We don't replace SAP (that would start a war). We build a fast, modern cockpit *on top of it*.

### The Goal: Digital Twin Cockpit
* **Read-Heavy, Write-Light:** Dispatch reads data from SAP ("The Senior Architect Way" via API or "The Hacker Way" via Excel exports) and visualizes it in a modern .NET interface
* **Work Distribution:** Real work management (assignment, prioritization) happens in Dispatch. Official registration happens in SAP

---

## 2. System Architecture

Dispatch functions as an "operational overlay" on top of legacy systems. It reads data, applies an intelligence layer, and provides a modern UI for operations management.

### Data Flow Architecture

```
┌─────────────────┐
│  Data Sources   │ (APIs, File Exports, Web Forms)
│ "System of Truth"│
└────────┬────────┘
         │ Periodic Sync (Read-Heavy)
         ▼
┌─────────────────┐
│   GERDA Engine  │ (Intelligence Layer)
│  G - Grouping   │ → Detect duplicates/related tickets
│  E - Estimating │ → Predict complexity
│  R - Ranking    │ → Prioritize by impact/urgency
│  D - Dispatching│ → Match tickets to agents
│  A - Anticipation│ → Forecast capacity needs
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Operations UI   │ (Modern Web Interface)
│ Team Dashboard  │
│ Agent Queues    │
│ Manager Alerts  │
└─────────────────┘
```

---

## 3. The Dispatch Intelligence Engine (G.E.R.D.A.)

The "Secret Sauce" is the AI-powered engine that optimizes the backlog based on 5 pillars:

### G — Grouping (The Noise Filter) 🧹

**Problem:** A frustrated citizen sends 5 emails about the same case, polluting the backlog.

**Solution:**
- **Implementation:** Rule-based clustering using LINQ queries
- **Logic:** Bundles cases from the same `TaxpayerId` and `Category` within a configurable time window (e.g., 7 days)
- **Win:** Less administrative noise, focus on content
- **Status:** ✅ **Implemented**

### E — Estimating (The Sizer) 📏

**Problem:** Not all cases are equal. An address change (5 min) is not a bankruptcy investigation (5 days).

**Solution:**
- **Implementation:** A mapping of `Case.Category` to Fibonacci points, augmented by keyword matching in the description
- **Technology:** Multi-Class Classification predicts "T-Shirt Size" (S, M, L, XL) based on historical data
- **Example Mapping:** `{"NTOF": 3, "ENRM": 8, "Fine": 1}`
- **Win:** Accurate estimation of work inventory (in hours, not just numbers)
- **Status:** ✅ **Implemented**

### R — Ranking (The Prioritizer) ⚖️

**Problem:** Agents do "cherry-picking" (easy cases first), causing difficult cases to miss their SLA.

**Solution:**
- **Implementation:** Weighted Shortest Job First (WSJF) algorithm
- **Formula:** 
  $$\text{Priority Score} = \frac{\text{Cost of Delay (SLA Breach Risk)}}{\text{Job Size (Fibonacci Points)}}$$
- **Cost of Delay Logic:** Calculated using a category-based `urgency_multiplier` and the number of days remaining until `SlaDeadline`. The cost increases exponentially as the deadline approaches
- **Win:** Cases with the highest ROI (most urgent + least effort) automatically rise to the top
- **Status:** ✅ **Implemented**

### D — Dispatching (The Matchmaker) 🤝

**Problem:** Knowledge is lost. New agents get cases from clients a senior has worked with for years.

**Solution:**
- **Implementation:** ML-driven recommendation engine using **ML.NET Matrix Factorization**
- **Affinity Score Logic:** The final recommendation score is a weighted blend of four factors:
  1. **Historical Affinity (40%):** ML prediction based on past successful agent-taxpayer pairings
  2. **Expertise Match (30%):** Matches agent `Specializations` list with the case `Category`
  3. **Language Match (20%):** Matches agent `Language` with taxpayer `Language`
  4. **Geographic Match (10%):** Matches agent `Region`
- **Constraint:** The model filters for agents with available capacity (`CurrentWorkload < MaxCapacity`) before scoring
- **Win:** Cases go to agents who already have context (or the right skills)
- **Status:** ✅ **Implemented**

### A — Anticipation (The Weather Report) 🌦️

**Problem:** Management is reactive. "Oops, the backlog exploded."

**Solution:**
- **Implementation:** Time series forecasting model using **ML.NET Singular Spectrum Analysis (SSA)**
- **Training Data:** 3 years of historical case inflow data, grouped by day/week
- **Logic:** The model decomposes seasonality and trend to forecast inflow for the next 90 days. An alert is triggered if `forecast_inflow > team_capacity`
- **Win:** Proactive warnings ("Director Alerts") *before* the crisis hits
- **Status:** ✅ **Implemented**

---

## 4. Technical Architecture

Dispatch is designed as a modular "White-Label" engine ("Ticket Masala").

### Technology Stack
| Layer | Technology | Rationale |
|---|---|---|
| **Backend** | .NET Core (C#) | Enterprise standard, stable. |
| **Database** | PostgreSQL / SQL Server | Relational structure, supported by Entity Framework ORM. |
| **AI/ML** | ML.NET | Native .NET framework for all intelligence modules. |
| **Frontend** | ASP.NET MVC / Blazor | Server-side rendering for robust internal tools. |
| **Data Ingestion** | File Watcher (.xlsx) → SAP APIs | Agile deployment with a path to production-grade integration. |

### Design Principles

- **Core:** .NET MVC/Core application (Enterprise grade, stable)
- **Config:** `masala_config.json` (or `gerda_config.json`) determines the rules. This allows Dispatch to be deployed for Hotel Tax today, but also for IT Helpdesk or HR tomorrow without rewriting code
- **Project Hierarchy:** Works with "Workstreams" (e.g., Appeals, Collection) instead of simple lists

### Implementation Status

| Component | Status | Technology / Algorithm |
|---|---|---|
| **G - Grouping** | ✅ **Implemented** | Rule-based LINQ |
| **E - Estimating**| ✅ **Implemented** | Category lookup & keyword matching |
| **R - Ranking** | ✅ **Implemented** | WSJF algorithm |
| **D - Dispatching**| ✅ **Implemented** | ML.NET Matrix Factorization |
| **A - Anticipation**| ✅ **Implemented** | ML.NET Time Series SSA |

### Simplified Data Model
```csharp
// Core Case Entity
public class Case
{
    public int Id { get; set; }
    public string CaseNumber { get; set; }
    public string TaxpayerId { get; set; }
    public string Category { get; set; }
    public DateTime SubmissionDate { get; set; }
    public DateTime SlaDeadline { get; set; }
    public string Status { get; set; }
    public int? AssignedAgentId { get; set; }
    public int ComplexityPoints { get; set; }
    public decimal PriorityScore { get; set; }
}

// Core Agent Entity
public class Agent
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Language { get; set; }
    public List<string> Specializations { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentWorkload { get; set; }
}
```
---

## 5. Configuration Management

All business rules, weights, and settings are externalized to a `gerda_config.json` (or `masala_config.json`) file. This allows the engine to be adapted for new domains without code changes - the key to the "White-Label" approach.

**Example `gerda_config.json` snippet:**
```json
{
  "work_queues": [
    {
      "name": "Hotel Tax",
      "categories": ["NTOF", "ENRM", "Fine", "Declaration"],
      "sla_defaults": { "NTOF": 30, "Fine": 60, "ENRM": 90 },
      "urgency_multipliers": { "NTOF": 2.0, "Fine": 3.0, "ENRM": 2.5 }
    }
  ],
  "gerda_modules": {
    "dispatching": {
      "enabled": true,
      "affinity_weights": {
        "past_interaction": 0.4,
        "category_expertise": 0.3,
        "language_match": 0.2,
        "geography": 0.1
      }
    },
    "anticipation": {
      "enabled": true,
      "forecast_horizon_days": 90,
      "alert_threshold": 0.3
    }
  }
}
```

---

## 6. Success Metrics (KPIs)

### Operational KPIs
| Metric | Baseline (Manual) | Target (GERDA) |
|---|---|---|
| **Time to First Action** | 5-7 days | < 2 days |
| **SLA Breach Rate** | 15% | < 5% |
| **Agent Workload Balance** | Std Dev = 12 points | Std Dev < 5 points |
| **Coordinator Assignment Time**| 8 hours/week | < 1 hour/week |

### Financial & Quality KPIs
| Metric | Target |
|---|---|
| **Yield per Agent Hour** | +30% |
| **Revenue Detected (vs Manual)**| +77% (proven in Airbnb pilot) |
| **Recommendation Acceptance Rate**| > 85% |
| **Forecast Accuracy (MAE)** | < 15% error |

---

## 7. Stakeholder Impact & Political Framing

> [!tip] Political Framing
> To IT/SAP Administrators: *"You manage the vault (SAP). We manage the logistics at the front end. We don't touch your data, we just make it visible."*

### Stakeholder Benefits

- **For Team Leads:** Finally get a grip. See who's doing what
- **For Agents:** Fairer distribution. No more fights over who gets the "crappy tasks" (GERDA decides fairly via WSJF)
- **For Management:** Predictability. Dashboarding based on facts, not gut feeling

---

## 8. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| **Data Sync Lag** | Because we're an overlay, data sometimes lags behind SAP | Clearly communicate "Last Sync Time" in the UI |
| **Agents resist automated assignment** | Low adoption | Human-in-the-loop design (override is possible); training focused on "augmentation, not replacement" |
| **Model drift (predictions decay)** | Poor prioritization | Monthly automated retraining; accuracy monitoring dashboards |
| **Data quality issues** | Inaccurate scoring | Data validation layer at ingestion; alerts for incomplete records; fallback to default scores |
| **Algorithmic Bias (expert burnout)** | Top experts get all hard cases | Load balancing is built into the Dispatcher's scoring (affinity score is penalized by current workload) |

---

## 9. Next Actions

- [ ] Configure the `masala_config.json` for the Hotel Tax pilot
- [ ] Test the "Hacker Way" ingest (read Excel export from SAP)
- [ ] Show dashboard mockup to Didier (focus on the "Anticipation" graph)
- [ ] Deploy pilot with Hotel Tax team

---

## Appendix A: Tax Domain Glossary

| Term | Definition |
|---|---|
| **BP (Business Partner)** | Taxpayer entity in SAP. |
| **NTOF** | *Notification de taxation d'office* (automatic taxation for a missing declaration). |
| **ENRM** | *Enrolment correctif* (taxpayer-requested correction). |
| **Qlik** | Business intelligence platform; initial source of data exports. |
| **Yield** | Revenue recovered per unit of agent effort. |
