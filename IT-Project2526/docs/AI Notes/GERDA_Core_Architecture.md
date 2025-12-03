# GERDA — Core Architecture Specification

**Project:** Generic Extended Resource Dispatch & Anticipation  
**Version:** 1.0  
**Date:** December 2, 2024  
**Purpose:** Domain-agnostic ticketing system with AI-powered operations

---

## Executive Summary

GERDA is an AI-powered operations cockpit designed to automate work allocation, prioritization, and capacity planning for ticketing systems. It sits as an "operational overlay" on top of existing legacy systems to provide modern work management capabilities without replacing core infrastructure.

**Core Objective:** Transform manual ticket distribution into an industrialized, data-driven dispatch system that maximizes throughput while protecting agent capacity.

---

## 1. Problem Statement

### Universal Ticketing Challenges

| Challenge | Impact |
|-----------|--------|
| **Manual Ticket Assignment** | Team coordinators spend hours manually assigning tickets to agents |
| **No Prioritization Logic** | Tickets handled FIFO or by agent preference, not by impact or urgency |
| **Lost Institutional Knowledge** | When experienced agents leave, their customer relationships and domain expertise disappear |
| **Reactive Capacity Planning** | Backlogs discovered too late; no forecasting for workload spikes |
| **Cherry-Picking** | Agents may select easy tickets, leaving complex high-priority tickets unresolved |

---

## 2. Solution Architecture

### Design Philosophy: Digital Twin Operations Cockpit

GERDA provides:

- **Work visibility:** Real-time view of backlog, agent capacity, SLA status
- **Intelligent dispatch:** Automated ticket assignment based on complexity, urgency, and agent fit
- **Predictive planning:** Forecasting bottlenecks before they occur

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

## 3. The GERDA Intelligence Stack

### G — Grouping (Noise Filter)

**Problem:** Repetitive requests from same customer clog queues

**Solution:** Clustering algorithm detects duplicate/related tickets and bundles them into a parent ticket

**Algorithm:**
- K-Means clustering on `customer_id` + `category` + `submission_date`
- Rule-based grouping: if `customer_id` + `category` appears >3 times in configurable window → bundle

**Output:** Reduced noise; agents handle one consolidated ticket instead of multiple fragments

**Generic Example:**
```
Input: 
  - Ticket #1001: Customer C123, Category "Billing", 2024-11-15
  - Ticket #1002: Customer C123, Category "Billing", 2024-11-16
  - Ticket #1003: Customer C123, Category "Billing", 2024-11-17

Output:
  - Parent Ticket #1001 (contains #1002, #1003 as sub-tickets)
```

**Configuration:**
```json
{
  "grouping": {
    "enabled": true,
    "algorithm": "k_means",
    "time_window_days": 7,
    "threshold": 3
  }
}
```

---

### E — Estimating (The Sizer)

**Problem:** Not all tickets are equal in complexity and effort required

**Solution:** Multi-class classifier predicts complexity "T-Shirt Size" based on historical data

**Training Data:**
- Features: `category`, `customer_history_length`, `description_length`, `attachment_count`
- Label: Cycle time quantile → mapped to size (S, M, L, XL)

**Size → Fibonacci Points Mapping:**
- **S (Simple):** 1 point (quick resolution, standard process)
- **M (Medium):** 3 points (moderate complexity, some investigation)
- **L (Large):** 8 points (complex issue, multiple stakeholders)
- **XL (Extra Large):** 13 points (major investigation, cross-team coordination)

**Models Supported:**
- Lookup table (category-based)
- Random Forest classifier
- Gradient Boosting

**Output:** Every incoming ticket receives a complexity score for capacity planning

**Configuration:**
```json
{
  "estimating": {
    "enabled": true,
    "model_type": "random_forest",
    "model_path": "/models/complexity_classifier.pkl",
    "features": ["category", "customer_history", "description_length"],
    "size_mapping": {
      "S": 1,
      "M": 3,
      "L": 8,
      "XL": 13
    }
  }
}
```

---

### R — Ranking (The Prioritizer)

**Problem:** Agents cherry-pick easy tasks; urgent tickets breach SLAs

**Solution:** Weighted Shortest Job First (WSJF) algorithm

**Formula:**
$$\text{Priority Score} = \frac{\text{Cost of Delay}}{\text{Job Size (Fibonacci Points)}}$$

**Cost of Delay Calculation:**
```python
days_until_sla_breach = sla_deadline - today

# Category-based urgency multiplier (configurable)
urgency_multiplier = category_urgency_map.get(ticket.category, 1.0)

# SLA urgency increases as deadline approaches
if days_until_sla_breach <= 0:
    sla_urgency = 10.0  # Already breached
elif days_until_sla_breach <= 1:
    sla_urgency = 5.0   # Critical
elif days_until_sla_breach <= 3:
    sla_urgency = 2.0   # High
else:
    sla_urgency = urgency_multiplier / max(days_until_sla_breach, 1)

cost_of_delay = sla_urgency
```

**Generic Example:**
```
Ticket A: Support, 2 days until breach, Size = 1 point, Urgency = 2.0
  → Cost of Delay = 2.0
  → Priority = 2.0 / 1 = 2.0

Ticket B: Feature Request, 10 days until breach, Size = 8 points, Urgency = 1.0
  → Cost of Delay = 1.0 / 10 = 0.1
  → Priority = 0.1 / 8 = 0.0125

Result: Ticket A (quick win, urgent) goes to top of queue
```

**Output:** Dynamically ranked work queue maximizing value per agent hour

**Configuration:**
```json
{
  "ranking": {
    "enabled": true,
    "algorithm": "wsjf",
    "urgency_multipliers": {
      "Critical": 3.0,
      "High": 2.0,
      "Medium": 1.5,
      "Low": 1.0
    }
  }
}
```

---

### D — Dispatching (The Matchmaker)

**Problem:** New agents lack context on customers with long history; experienced agents become overloaded

**Solution:** Recommendation engine using collaborative filtering

**Affinity Scoring (4 Factors):**

```python
affinity_score = (
    0.4 * past_interaction_score +      # Has agent handled this customer before?
    0.3 * category_expertise_score +    # Is agent skilled in this ticket type?
    0.2 * language_match_score +        # Does agent speak customer's language?
    0.1 * geographic_proximity_score    # Is agent assigned to this region?
)

# Capacity constraint
available_agents = [a for a in agents if a.current_workload < a.max_capacity]

# Recommendation
best_agent = max(available_agents, key=lambda a: affinity_score(a, ticket))
```

**Machine Learning Approach:**
- **Matrix Factorization** for past interaction patterns
- Implicit ratings based on resolution time (fast = good fit)
- Workload balancing to prevent overload

**Generic Example:**
```
Ticket: Customer C999 (English speaker, 3-year history, billing issue)

Agent A: Has handled C999 before (5 tickets), billing specialist, 5/10 capacity
  → Affinity = 0.4*1.0 + 0.3*0.9 + 0.2*1.0 + 0.1*0.8 = 0.75

Agent B: No history with C999, generalist, 2/10 capacity
  → Affinity = 0.4*0.0 + 0.3*0.4 + 0.2*1.0 + 0.1*0.8 = 0.40

Recommendation: Assign to Agent A (preserves institutional knowledge)
```

**Output:** Optimal agent-ticket pairing; reduced onboarding time; better customer experience

**Configuration:**
```json
{
  "dispatching": {
    "enabled": true,
    "affinity_weights": {
      "past_interaction": 0.4,
      "category_expertise": 0.3,
      "language_match": 0.2,
      "geography": 0.1
    },
    "ml_model": "matrix_factorization",
    "max_assigned_tickets_per_agent": 10
  }
}
```

---

### A — Anticipation (The Weather Report)

**Problem:** Teams react to backlogs instead of preventing them

**Solution:** Time series forecasting for proactive capacity planning

**Data:**
- Historical incoming ticket volume by day/week/month
- Seasonal patterns (e.g., end-of-quarter spikes, holiday dips)
- Team capacity (agent count, average velocity, planned leave)

**Models Supported:**
- Singular Spectrum Analysis (SSA)
- Prophet (Facebook's time series forecaster)
- ARIMA

**Forecasting Horizon:** Configurable (default: 30-90 days)

**Alert Logic:**
```python
forecast_inflow = predict_tickets_next_N_days()
forecast_capacity = team_velocity * available_agent_days

bottleneck_risk = forecast_inflow - forecast_capacity

if bottleneck_risk > threshold:
    alert_manager(
        severity="High",
        message=f"Predicted backlog overflow in {weeks_ahead} weeks",
        recommendation="Consider temporary staffing or priority shift"
    )
```

**Generic Example:**
```
Current Date: 2024-11-01
Forecast: 
  - Week of 2024-12-15: +250% ticket volume (holiday shopping season)
  - Team capacity: 150 tickets/week
  - Predicted inflow: 375 tickets/week

Alert: "Critical bottleneck predicted for Week 50 (Dec 15). 
        Recommend: Pause non-urgent feature requests; activate on-call rotation."
```

**Output:** Manager dashboard with early-warning alerts; proactive resource allocation

**Configuration:**
```json
{
  "anticipation": {
    "enabled": true,
    "forecast_horizon_days": 90,
    "model": "ssa",
    "alert_threshold_percentage": 0.3,
    "min_history_days": 90
  }
}
```

---

## 4. Technical Stack

### Technology Choices

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| **Backend** | .NET 10 (C#) | Cross-platform, enterprise-grade, Entity Framework ORM |
| **Database** | PostgreSQL / SQL Server / SQLite | Relational structure for ticket management |
| **AI/ML** | ML.NET | Native .NET ML framework (alternative: Python microservice with FastAPI) |
| **Frontend** | ASP.NET MVC / Blazor | Server-side rendering for internal tools; minimal JavaScript |
| **Data Ingestion** | File Watcher / REST API / Webhooks | Flexible integration options |
| **Config Management** | JSON configuration files | White-label design; domain rules externalized |

---

## 5. Data Model (Core Entities)

```csharp
public class Ticket 
{
    public Guid Guid { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string CustomerId { get; set; }
    public string Category { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime SlaDeadline { get; set; }
    public Status TicketStatus { get; set; }  // Open, Assigned, InProgress, Completed
    public string? ResponsibleId { get; set; }  // Assigned agent
    
    // GERDA Fields
    public int EstimatedEffortPoints { get; set; }  // From Estimator
    public double PriorityScore { get; set; }       // From Ranker
    public string? GerdaTags { get; set; }          // AI-generated tags
    public int? ProjectId { get; set; }             // Optional: work queue grouping
}

public class Employee : ApplicationUser
{
    public string Team { get; set; }
    public int Level { get; set; }
    
    // GERDA Extensions
    public string? Language { get; set; }           // Primary language
    public string? Specializations { get; set; }    // JSON array of expertise areas
    public int MaxCapacityPoints { get; set; }      // Fibonacci capacity
    public string? Region { get; set; }             // Geographic assignment
}

public class Customer : ApplicationUser
{
    public string CompanyName { get; set; }
    public string? Language { get; set; }
    public string? Region { get; set; }
}

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<Ticket> Tickets { get; set; }
}

public class GerdaMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string MetricName { get; set; }
    public double Value { get; set; }
    public string? Category { get; set; }
    public string? AgentId { get; set; }
}
```

---

## 6. Configuration Schema

### Master Configuration: `gerda_config.json`

```json
{
  "application": {
    "name": "GERDA Ticketing System",
    "environment": "production",
    "version": "1.0.0"
  },
  
  "data_sources": {
    "ingestion_mode": "file_watcher",
    "file_watcher_path": "/data/imports/",
    "file_pattern": "*.xlsx",
    "api_endpoint": null
  },
  
  "work_queues": [
    {
      "id": 1,
      "name": "General Support",
      "categories": ["Bug", "Feature Request", "Question", "Complaint"],
      "sla_defaults": {
        "Bug": 48,
        "Feature Request": 168,
        "Question": 24,
        "Complaint": 72
      },
      "urgency_multipliers": {
        "Bug": 2.5,
        "Feature Request": 1.0,
        "Question": 1.5,
        "Complaint": 3.0
      }
    }
  ],
  
  "gerda_modules": {
    "grouping": {
      "enabled": true,
      "algorithm": "k_means",
      "time_window_days": 7,
      "threshold": 3
    },
    "estimating": {
      "enabled": true,
      "model_type": "lookup_table",
      "category_complexity": {
        "Bug": 3,
        "Feature Request": 8,
        "Question": 1,
        "Complaint": 5
      }
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
      },
      "max_assigned_tickets_per_agent": 10
    },
    "anticipation": {
      "enabled": true,
      "forecast_horizon_days": 30,
      "model": "ssa",
      "alert_threshold_percentage": 0.3,
      "min_history_days": 90
    }
  }
}
```

---

## 7. Success Metrics (Generic)

### Operational KPIs

| Metric | Typical Baseline | GERDA Target |
|--------|------------------|--------------|
| **Time to First Action** | 3-5 days | <1 day |
| **SLA Breach Rate** | 10-20% | <5% |
| **Agent Workload Balance** | High variance | Low variance (Std Dev <5 points) |
| **Manager Assignment Time** | 5-10 hours/week | <1 hour/week |
| **Tickets Handled per Agent** | Variable | +30% (complexity-adjusted) |

### Quality KPIs

| Metric | Target |
|--------|--------|
| **Recommendation Acceptance Rate** | >80% |
| **Forecast Accuracy (MAE)** | <15% error |
| **False Positive Grouping Rate** | <5% |
| **Agent Satisfaction** | >4/5 |

---

## 8. Deployment Strategy

### Phase 1: Proof of Concept (4 weeks)

| Week | Deliverable |
|------|-------------|
| 1 | Data ingestion pipeline; tickets populate GERDA database |
| 2 | Grouping (G) + Estimating (E) modules active |
| 3 | Ranking (R) module deployed; work queue auto-prioritized |
| 4 | Basic UI: agents see prioritized queue; manager sees team dashboard |

### Phase 2: Enhanced Features (4 weeks)

| Week | Deliverable |
|------|-------------|
| 5-6 | Dispatching (D) with affinity scoring |
| 7-8 | Anticipation (A) with forecasting and alerts |

### Phase 3: Production Hardening (4 weeks)

| Week | Deliverable |
|------|-------------|
| 9-10 | Performance optimization, caching, indexing |
| 11-12 | Metrics dashboard, documentation, training |

---

## 9. UI Components

### Agent Queue View
- Prioritized ticket list (sorted by PriorityScore)
- Complexity indicators (Fibonacci points)
- SLA countdown timers
- Recommended tickets section
- "Accept Recommendation" / "Reject" actions

### Team Dashboard (Manager View)
- Workload distribution chart (agent capacity utilization)
- SLA compliance gauge
- Tickets by priority breakdown
- Capacity risk alerts
- Team velocity trends

### Director Analytics
- 30/60/90-day forecast visualization
- KPI trends (time to first action, breach rate)
- Agent performance metrics
- Category distribution analysis

---

## 10. Integration Patterns

### File Watcher (Phase 1)
```csharp
public class FileWatcherService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken ct)
    {
        var watcher = new FileSystemWatcher(_config.WatchPath)
        {
            Filter = _config.FilePattern,
            EnableRaisingEvents = true
        };
        
        watcher.Created += async (sender, e) =>
        {
            await ProcessImportFile(e.FullPath);
        };
        
        return Task.CompletedTask;
    }
}
```

### REST API (Phase 2)
```csharp
[HttpPost("api/tickets")]
public async Task<IActionResult> CreateTicket([FromBody] TicketCreateDto dto)
{
    var ticket = MapDtoToEntity(dto);
    await _context.Tickets.AddAsync(ticket);
    await _context.SaveChangesAsync();
    
    if (_gerdaService.IsEnabled)
    {
        await _gerdaService.ProcessTicketAsync(ticket.Guid);
    }
    
    return CreatedAtAction(nameof(GetTicket), new { id = ticket.Guid }, ticket);
}
```

### Webhook Integration
```csharp
[HttpPost("api/webhooks/ticket-created")]
public async Task<IActionResult> TicketCreatedWebhook([FromBody] WebhookPayload payload)
{
    var ticket = ParsePayload(payload);
    await _gerdaService.ProcessTicketAsync(ticket.Guid);
    return Ok();
}
```

---

## 11. Extensibility & Customization

### Domain-Specific Configurations

GERDA is designed to be white-labeled for different domains:

- **IT Support:** Categories = Bug/Feature/Question, SLA = 24-72 hours
- **Customer Service:** Categories = Complaint/Inquiry/Return, SLA = 12-48 hours
- **Healthcare:** Categories = Urgent/Standard/Admin, SLA = 2-24 hours
- **Tax Office:** (See separate TAX-specific configuration document)

### Custom Modules

Extend GERDA with domain-specific modules:

```csharp
public interface IGerdaModule
{
    Task ProcessTicketAsync(Guid ticketGuid);
    string ModuleName { get; }
    bool IsEnabled { get; }
}

// Example: Fraud Detection Module
public class FraudDetectionModule : IGerdaModule
{
    public async Task ProcessTicketAsync(Guid ticketGuid)
    {
        var ticket = await _context.Tickets.FindAsync(ticketGuid);
        var riskScore = await CalculateFraudRisk(ticket);
        
        if (riskScore > 0.8)
        {
            ticket.GerdaTags += ",HIGH-RISK";
            ticket.PriorityScore *= 2.0;  // Boost priority
        }
    }
}
```

---

## 12. Security & Privacy

### Data Protection
- All PII encrypted at rest (customer names, contact info)
- Role-based access control (RBAC) for GERDA insights
- Audit trail for all AI-driven assignments

### Model Governance
- Monthly retraining on closed tickets only
- No external data sources
- Explainable AI: show why each recommendation was made

### Compliance
- GDPR-compliant data retention (configurable TTL)
- Right to erasure supported
- Transparent AI decision-making

---

## 13. Performance Requirements

| Metric | Target |
|--------|--------|
| **Ticket Processing Time** | <2 seconds (G+E+R+D) |
| **Dashboard Load Time** | <500ms |
| **Forecast Calculation** | <10 seconds (90-day horizon) |
| **Concurrent Users** | 100+ agents |
| **Database Size** | 1M+ tickets supported |

---

## 14. Testing Strategy

### Unit Tests
- WSJF calculation logic
- Affinity scoring algorithm
- Complexity estimation accuracy

### Integration Tests
- Full GERDA workflow (ticket creation → assignment)
- File watcher import
- Metrics calculation

### Load Tests
- 10,000 ticket dataset
- Parallel agent queue requests
- Forecast calculation under load

### User Acceptance Tests
- Agent queue usability
- Manager dashboard clarity
- Override mechanisms

---

## 15. Documentation Deliverables

1. **User Guides**
   - Agent Guide: "How to use your AI-powered queue"
   - Manager Guide: "Understanding GERDA insights"
   - Admin Guide: "Configuring GERDA rules"

2. **Technical Documentation**
   - API documentation (Swagger/OpenAPI)
   - Architecture diagrams (C4 model)
   - Deployment guide (Docker, Kubernetes)

3. **Training Materials**
   - Video walkthrough
   - FAQ document
   - Troubleshooting guide

---

**Document Status:** Core Architecture v1.0  
**Next Steps:** Implement domain-specific configuration (see TAX Office example)
