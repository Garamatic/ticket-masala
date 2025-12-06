# GERDA — Specification Document

**Project:** GovTech Extended Resource Dispatch & Anticipation  
**Domain:** TAX Department Operations (Brussel Fiscaliteit)  
**Owner:** Juan Benjumea  
**Version:** 1.0  
**Date:** December 2, 2025

---

## Executive Summary

GERDA is an AI-powered operations cockpit designed to automate work allocation, prioritization, and capacity planning for the TAX department. It sits as an "operational overlay" on top of existing legacy systems (SAP, Qlik) to provide modern work management capabilities without replacing core infrastructure.

**Core Objective:** Transform artisanal, manual case distribution into an industrialized, data-driven dispatch system that maximizes fiscal yield while protecting agent capacity.

---

## 1. Problem Statement

### Current State: Manual Dispatch & Hidden Bottlenecks

The TAX department faces critical operational challenges:

| Challenge | Impact |
|-----------|--------|
| **Manual Case Assignment** | Team coordinators spend hours sorting through exports, manually assigning cases to agents |
| **No Prioritization Logic** | Cases handled FIFO or by agent preference, not by fiscal yield or SLA risk |
| **Lost Institutional Knowledge** | When experienced agents leave, their client relationships and domain expertise disappear |
| **Reactive Capacity Planning** | Backlogs discovered too late; no forecasting for seasonal spikes (e.g., declaration deadlines) |
| **Cherry-Picking** | Agents may select easy cases, leaving complex high-value cases to breach SLAs |

### The Cost

- **Airbnb pilot proof:** Manual triage identified 9,000 priority cases; automated analysis found 16,000 (+77%)
- **Agent burnout:** Repetitive sorting tasks create cognitive debt and stress
- **Revenue loss:** High-value recovery opportunities missed due to lack of visibility

---

## 2. Solution Architecture

### Design Philosophy: Digital Twin Operations Cockpit

GERDA is **not** a replacement for SAP. It is a fast, modern "cockpit" that provides:

- **Work visibility:** Real-time view of backlog, agent capacity, SLA status
- **Intelligent dispatch:** Automated case assignment based on complexity, urgency, and agent fit
- **Predictive planning:** Forecasting bottlenecks before they occur

### Data Flow Architecture

```
┌─────────────────┐
│  Legacy Systems │ (SAP, Qlik, PDF Spools)
│ "System of Truth"│
└────────┬────────┘
         │ Periodic Sync (Read-Heavy)
         ▼
┌─────────────────┐
│   GERDA Engine  │ (Intelligence Layer)
│  Grouping       │
│  Estimating     │
│  Ranking        │
│  Dispatching    │
│  Anticipation   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Operations UI   │ (Modern Web Interface)
│ Team Dashboard  │
│ Agent Queues    │
│ Director Alerts │
└─────────────────┘
```

### Integration Strategies

**Phase 1 (Hacker/Proof of Concept):**
- Qlik exports → `.xlsx` files dropped in monitored folder
- File watcher ingests data into GERDA database
- **Advantage:** Zero IT permissions needed, rapid deployment

**Phase 2 (Production/Architect):**
- Direct API integration (SAP .NET Connector, BAPIs)
- Real-time data sync
- **Advantage:** Robust, scalable, enterprise-grade

---

## 3. The GERDA Intelligence Stack

### G — Grouping (Noise Filter)

**Problem:** Repetitive requests from same taxpayer clog queues (e.g., one taxpayer submitting 15 rectification requests)

**Solution:** Clustering algorithm detects duplicate/related cases and bundles them into a parent ticket

**Algorithm:**
- K-Means clustering on taxpayer_id + case_category + submission_date
- Rule-based grouping: if taxpayer_id + category appears >3 times in 7-day window → bundle

**Output:** Reduced noise; agents handle one consolidated case instead of 15 fragments

**Tax Domain Example:**
```
Input: 
  - Case #1001: Taxpayer BP12345, NTOF reaction, 2024-11-15
  - Case #1002: Taxpayer BP12345, NTOF reaction, 2024-11-16
  - Case #1003: Taxpayer BP12345, NTOF reaction, 2024-11-17

Output:
  - Parent Case #1001 (contains #1002, #1003 as sub-tickets)
```

---

### E — Estimating (The Sizer)

**Problem:** Not all cases are equal; a late inscription fine is 5 minutes; a complex ENRM audit is 5 days

**Solution:** Multi-class classifier predicts complexity "T-Shirt Size" based on historical data

**Training Data:**
- Features: `case_category`, `taxpayer_history_length`, `amount_disputed`, `document_count`
- Label: Cycle time quantile → mapped to size (S, M, L, XL)

**Size → Fibonacci Points Mapping:**
- **S (Simple):** 1 point (e.g., late inscription fine, standard declaration)
- **M (Medium):** 3 points (e.g., NTOF reaction verification, rectification)
- **L (Large):** 8 points (e.g., ENRM audit, capacity dispute)
- **XL (Extra Large):** 13 points (e.g., multi-year fraud investigation)

**Model:** Random Forest or Gradient Boosting trained on past closed cases

**Output:** Every incoming case receives a complexity score for prioritization

---

### R — Ranking (The Prioritizer)

**Problem:** Agents cherry-pick easy tasks; urgent cases breach SLAs

**Solution:** Weighted Shortest Job First (WSJF) algorithm

**Formula:**
$$\text{Priority Score} = \frac{\text{Cost of Delay}}{\text{Job Size (Fibonacci Points)}}$$

**Cost of Delay Calculation:**
```python
days_until_sla_breach = sla_deadline - today
urgency_multiplier = {
    "Fine": 3,           # High revenue impact
    "NTOF": 2,           # Legal obligation
    "Declaration": 1.5,  # Standard workflow
    "ENRM": 2.5          # Taxpayer-requested, reputational risk
}

cost_of_delay = urgency_multiplier / max(days_until_sla_breach, 1)
```

**Tax Domain Example:**
```
Case A: Fine, 2 days until breach, Size = 1 point
  → Cost of Delay = 3 / 2 = 1.5
  → Priority = 1.5 / 1 = 1.5

Case B: ENRM, 10 days until breach, Size = 8 points
  → Cost of Delay = 2.5 / 10 = 0.25
  → Priority = 0.25 / 8 = 0.03

Result: Case A (quick win, urgent) goes to top of queue
```

**Output:** Dynamically ranked work queue maximizing fiscal yield per agent hour

---

### D — Dispatching (The Matchmaker)

**Problem:** New agents lack context on taxpayers with long history; experienced agents become overloaded

**Solution:** Recommendation engine using Matrix Factorization

**Data:**
- Agent-Taxpayer interaction matrix (past cases handled)
- Agent specialization scores (trained on case categories)
- Current agent workload (active cases, total Fibonacci points)

**Algorithm:**
```python
affinity_score = (
    0.4 * past_interaction_score +      # Has agent handled this taxpayer before?
    0.3 * category_expertise_score +    # Is agent skilled in this case type?
    0.2 * language_match_score +        # Does agent speak taxpayer's language?
    0.1 * geographic_proximity_score    # Is agent assigned to this region?
)

# Capacity constraint
available_agents = [a for a in agents if a.current_workload < a.max_capacity]

# Recommendation
best_agent = max(available_agents, key=lambda a: affinity_score(a, case))
```

**Tax Domain Example:**
```
Case: Taxpayer BP99999 (Dutch speaker, 5-year history, hotel tax ENRM)

Agent A: Has handled BP99999 before (3 cases), hotel tax specialist, 5/10 capacity
  → Affinity = 0.4*1.0 + 0.3*0.9 + 0.2*1.0 + 0.1*0.8 = 0.75

Agent B: No history with BP99999, generalist, 2/10 capacity
  → Affinity = 0.4*0.0 + 0.3*0.4 + 0.2*1.0 + 0.1*0.8 = 0.40

Recommendation: Assign to Agent A (preserves institutional knowledge)
```

**Output:** Optimal agent-case pairing; reduced onboarding time; better client experience

---

### A — Anticipation (The Weather Report)

**Problem:** Teams react to backlogs instead of preventing them

**Solution:** Time series forecasting for proactive capacity planning

**Data:**
- Historical incoming case volume by week/month (past 3 years)
- Seasonal patterns (e.g., declaration deadlines → spikes in NTOF, rectifications)
- Team capacity (agent count, average velocity, planned leave)

**Model:** Singular Spectrum Analysis (SSA) or Prophet for decomposing seasonality + trend

**Forecasting Horizon:** 90 days ahead

**Alert Logic:**
```python
forecast_inflow = predict_cases_next_90_days()
forecast_capacity = team_velocity * available_agent_days

bottleneck_risk = forecast_inflow - forecast_capacity

if bottleneck_risk > threshold:
    alert_director(
        severity="High",
        message=f"Predicted backlog overflow in {weeks_ahead} weeks",
        recommendation="Consider temporary staffing or priority shift"
    )
```

**Tax Domain Example:**
```
Current Date: 2024-11-01
Forecast: 
  - Week of 2024-12-15: +350% case volume (year-end declaration deadline)
  - Team capacity: 200 cases/week
  - Predicted inflow: 700 cases/week

Alert: "Critical bottleneck predicted for Week 50 (Dec 15). 
        Recommend: Pause non-urgent ENRM audits; activate temporary contractors."
```

**Output:** Director dashboard with early-warning alerts; proactive resource allocation

---

## 4. Technical Stack

### Technology Choices

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| **Backend** | .NET Core (C#) | Enterprise standard, SAP .NET Connector support, university thesis alignment |
| **Database** | PostgreSQL or SQL Server | Relational structure for case management; Entity Framework ORM |
| **AI/ML** | Python microservice (FastAPI) | Scikit-learn, Prophet for models; .NET calls via REST API |
| **Frontend** | ASP.NET MVC / Blazor | Server-side rendering for internal tools; minimal JavaScript |
| **Data Ingestion** | File Watcher (.xlsx) → Phase 2: SAP APIs | Agile deployment; upgrade path to production |
| **Config Management** | `gerda_config.json` | White-label design; domain rules externalized |

### Data Model (Simplified)

```csharp
// Core Entities

public class Case 
{
    public int Id { get; set; }
    public string CaseNumber { get; set; }  // SAP case number
    public string TaxpayerId { get; set; }  // BP number
    public string Category { get; set; }    // NTOF, ENRM, Fine, Declaration
    public DateTime SubmissionDate { get; set; }
    public DateTime SlaDeadline { get; set; }
    public string Status { get; set; }      // Open, Assigned, InProgress, Closed
    public int? AssignedAgentId { get; set; }
    public int ComplexityPoints { get; set; }  // Fibonacci points from Estimator
    public decimal PriorityScore { get; set; } // From Ranker
}

public class Agent
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Language { get; set; }
    public List<string> Specializations { get; set; }  // Categories they excel in
    public int MaxCapacity { get; set; }  // Max Fibonacci points they can handle
    public int CurrentWorkload { get; set; }
}

public class WorkQueue
{
    public int Id { get; set; }
    public string Name { get; set; }  // "Hotel Tax", "VAT", "PrI"
    public List<Case> Cases { get; set; }
}

public class Forecast
{
    public DateTime Week { get; set; }
    public int PredictedCaseVolume { get; set; }
    public int TeamCapacity { get; set; }
    public string AlertLevel { get; set; }  // None, Warning, Critical
}
```

---

## 5. Configuration: `gerda_config.json`

White-label design allows GERDA to adapt to different tax products or departments.

```json
{
  "application": {
    "name": "GERDA - Hotel Tax Pilot",
    "environment": "production"
  },
  "data_sources": {
    "ingestion_mode": "file_watcher",  // or "api"
    "file_watcher_path": "/data/qlik_exports/",
    "file_pattern": "*.xlsx",
    "sap_connector": {
      "enabled": false,
      "endpoint": "sap.bf.brussels/api",
      "credentials": "encrypted_key"
    }
  },
  "work_queues": [
    {
      "name": "Hotel Tax",
      "categories": ["NTOF", "ENRM", "Fine", "Declaration", "Rectification"],
      "sla_defaults": {
        "NTOF": 30,
        "Fine": 60,
        "ENRM": 90,
        "Declaration": 45,
        "Rectification": 30
      },
      "urgency_multipliers": {
        "NTOF": 2.0,
        "Fine": 3.0,
        "ENRM": 2.5,
        "Declaration": 1.5,
        "Rectification": 1.5
      }
    }
  ],
  "gerda_modules": {
    "grouping": {
      "enabled": true,
      "algorithm": "k_means",
      "threshold": 3  // Group if >3 cases from same taxpayer in 7 days
    },
    "estimating": {
      "enabled": true,
      "model_path": "/models/complexity_classifier.pkl",
      "features": ["category", "taxpayer_history", "amount", "document_count"]
    },
    "ranking": {
      "enabled": true,
      "algorithm": "wsjf"
    },
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
      "model": "prophet",
      "alert_threshold": 0.3  // Alert if >30% capacity overflow predicted
    }
  },
  "agents": [
    {
      "id": 1,
      "name": "Agent A",
      "language": "NL",
      "specializations": ["Hotel Tax", "NTOF"],
      "max_capacity": 40  // Fibonacci points
    }
  ]
}
```

---

## 6. Deployment Strategy

### Phase 1: Proof of Concept (30 days)

**Objective:** Demonstrate value on Hotel Tax pilot with minimal infrastructure

| Week | Deliverable |
|------|-------------|
| 1 | File watcher ingests Qlik export; cases populate GERDA database |
| 2 | Grouping (G) + Estimating (E) modules active; complexity scores assigned |
| 3 | Ranking (R) module deployed; work queue auto-prioritized |
| 4 | Basic UI: agents see prioritized queue; coordinator sees team dashboard |

**Success Metric:** Coordinators spend 50% less time on manual assignment

---

### Phase 2: Controlled Rollout (60 days)

**Objective:** Add Dispatching (D) and Anticipation (A); validate with Key Users

| Week | Deliverable |
|------|-------------|
| 5-6 | Dispatching (D) with affinity scoring; agents receive recommended cases |
| 7-8 | Anticipation (A) with 90-day forecast; director alerts enabled |
| 9-10 | Human-in-the-loop validation: agents can override recommendations; feedback collected |
| 11-12 | Metrics dashboard: yield per agent, SLA compliance, forecast accuracy |

**Success Metric:** 
- 90% of auto-assignments accepted by agents
- Director receives 2+ proactive alerts preventing bottlenecks

---

### Phase 3: Production & Scale (90+ days)

**Objective:** Migrate to API-based ingestion; extend to other tax products

| Milestone | Action |
|-----------|--------|
| API Integration | Replace file watcher with SAP .NET Connector |
| Multi-Queue | Add PrI, TC, LEZ work queues with separate configs |
| Advanced Analytics | Add yield forecasting, agent performance trends |
| Mobile Access | Agents access queue via tablet/phone |

---

## 7. Success Metrics

### Operational KPIs

| Metric | Baseline (Manual) | Target (GERDA) |
|--------|-------------------|----------------|
| **Time to First Action** | 5-7 days | <2 days |
| **SLA Breach Rate** | 15% | <5% |
| **Agent Workload Balance** | Std Dev = 12 points | Std Dev <5 points |
| **Coordinator Assignment Time** | 8 hours/week | <1 hour/week |
| **Cases Handled per Agent** | 20/month | 30/month (complexity-adjusted) |

### Financial KPIs

| Metric | Baseline | Target |
|--------|----------|--------|
| **Yield per Agent Hour** | €X | +30% |
| **High-Value Cases Processed** | 60% | 90% |
| **Revenue Detected (vs Manual)** | 100% | +77% (proven in pilot) |

### Quality KPIs

| Metric | Target |
|--------|--------|
| **Recommendation Acceptance Rate** | >85% |
| **Forecast Accuracy (MAE)** | <15% error |
| **False Positive Grouping Rate** | <5% |

---

## 8. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Agents resist automated assignment** | Low adoption | Human-in-the-loop design; agents can override; training sessions emphasizing "augmentation not replacement" |
| **Model drift (complexity predictions decay)** | Poor prioritization | Monthly retraining on closed cases; monitoring dashboard for prediction accuracy |
| **Data quality issues (missing fields in Qlik exports)** | Inaccurate scoring | Data validation layer; alerts for incomplete records; fallback to manual assignment |
| **SAP API access denied by IT** | Blocked production upgrade | Phase 1 file watcher proves value first; business case for API access strengthened by pilot results |
| **Forecast model overfits to past seasonality** | Inaccurate alerts | Use ensemble methods (Prophet + SSA); validate against holdout period |

---

## 9. Governance & Ownership

| Role | Responsibility |
|------|----------------|
| **Product Owner (Juan)** | Define features, prioritize backlog, deliver to Key Users |
| **Data Scientist (TBD)** | Train ML models (Estimator, Anticipation), validate accuracy |
| **Backend Developer (TBD)** | Build .NET API, Entity Framework models, SAP integration |
| **Key Users (Hotel Team)** | Validate rules, test queue logic, provide feedback |
| **Sponsor (Didier)** | Resource allocation, stakeholder management, escalation |

**Weekly Cadence:**
- Sprint review with Key Users (Fridays, 30 min)
- Tech sync (Data Scientist + Dev, Tuesdays, 1 hour)
- Director briefing (monthly, 15 min)

---

## 10. Next Steps

### Immediate (This Week)
1. ✅ Finalize this specification
2. ⏳ Set up Git repository (`bf-automation/GERDA/`)
3. ⏳ Identify Data Scientist and Backend Developer resources
4. ⏳ Export first Qlik `.xlsx` sample for file watcher testing

### Week 1-2
1. Scaffold .NET project structure (MVC, Entity Framework)
2. Build file watcher ingestion pipeline
3. Create `Case` and `Agent` database models
4. Deploy mock data generator (Bogus library) for testing

### Week 3-4
1. Implement Grouping (G) module with K-Means
2. Implement Estimating (E) module with Random Forest
3. Build basic UI: prioritized case list view
4. Key User demo session #1

### Week 5-8
1. Deploy Ranking (R) with WSJF
2. Deploy Dispatching (D) with affinity scoring
3. Build team dashboard for coordinators
4. Key User validation & feedback collection

### Week 9-12
1. Implement Anticipation (A) with Prophet forecasting
2. Build director alerts dashboard
3. Generate 90-day operational report
4. Go/No-Go decision for production API integration

---

## Appendix A: Synthetic Data Generation Strategy

To protect privacy while developing/testing GERDA, use **Bogus** library to generate realistic synthetic data.

### Patterns to Inject

1. **Spam Pattern:** Taxpayer `BP-SPAM-001` submits 20 NTOF reactions in one week (should trigger Grouping)
2. **Complexity Pattern:** All `ENRM` cases have 3x longer cycle time than `Fine` cases (trains Estimator)
3. **Seasonality Pattern:** Case volume spikes by 400% in week 50 every year (trains Anticipation)
4. **Agent Affinity Pattern:** Agent "Marie" has handled 80% of Taxpayer `BP-VIP-042` past cases (optimizes Dispatcher)

### Sample Bogus Code

```csharp
var faker = new Faker<Case>()
    .RuleFor(c => c.CaseNumber, f => f.Random.AlphaNumeric(10))
    .RuleFor(c => c.TaxpayerId, f => f.PickRandom(taxpayerPool))
    .RuleFor(c => c.Category, f => f.PickRandom(categories))
    .RuleFor(c => c.SubmissionDate, f => f.Date.Recent(365))
    .RuleFor(c => c.SlaDeadline, (f, c) => c.SubmissionDate.AddDays(slaMap[c.Category]));

var cases = faker.Generate(10000);
```

---

## Appendix B: Tax Domain Glossary

| Term | Definition |
|------|------------|
| **BP (Business Partner)** | Taxpayer entity in SAP |
| **OC (Contract Object)** | Hotel/Airbnb establishment linked to BP |
| **NTOF** | Notification de taxation d'office (automatic full taxation for missing declaration) |
| **ENRM** | Enrolment correctif (taxpayer-requested correction to tax bill) |
| **Qlik** | Business intelligence platform; source of operational exports |
| **SCASEPS** | SAP inbox for taxpayer interactions |
| **Yield** | Revenue recovered per unit of agent effort |

---

**Document Status:** Draft for Review  
**Next Revision:** Post-Key User Feedback (Week 4)
