# GERDA AI Modules

Complete documentation for the GERDA AI system in Ticket Masala.

## Overview

**GERDA** = **G**roups, **E**stimates, **R**anks, **D**ispatches, **A**nticipates

GERDA is the AI-powered automation pipeline that processes tickets and provides intelligent recommendations.

```
New Ticket Created
        ↓
┌───────────────────────────────────────────┐
│              GERDA Pipeline               │
├───────────────────────────────────────────┤
│ G │ Grouping    → Spam detection, merge   │
│ E │ Estimating  → Effort points           │
│ R │ Ranking     → Priority score (WSJF)   │
│ D │ Dispatching → Agent recommendation    │
│ A │ Anticipation → Capacity forecasting   │
└───────────────────────────────────────────┘
        ↓
Ticket Enriched with AI Data
```

---

## Architecture

### GerdaService (Orchestrator)

**Location:** `Engine/GERDA/GerdaService.cs`

The main entry point that coordinates all GERDA modules.

```csharp
public class GerdaService : IGerdaService
{
    private readonly IGroupingService _groupingService;
    private readonly IEstimatingService _estimatingService;
    private readonly IRankingService? _rankingService;
    private readonly IDispatchingService? _dispatchingService;
    private readonly IAnticipationService? _anticipationService;

    public async Task ProcessTicketAsync(Guid ticketGuid)
    {
        // G - Grouping
        await _groupingService.CheckAndGroupTicketAsync(ticketGuid);
        
        // E - Estimating
        await _estimatingService.EstimateComplexityAsync(ticketGuid);
        
        // R - Ranking
        if (_rankingService?.IsEnabled == true)
            await _rankingService.CalculatePriorityScoreAsync(ticketGuid);
        
        // D - Dispatching
        if (_dispatchingService?.IsEnabled == true)
            await _dispatchingService.GetRecommendedAgentAsync(ticketGuid);
    }
}
```

---

## G - Grouping (Noise Filter)

Detects and merges duplicate or spam tickets.

### Implementation

**Interface:** `IGroupingService`  
**Location:** `Engine/GERDA/Grouping/`

**Algorithm:**
1. Compute `SHA256(Description + CustomerId)` as content hash
2. Query for existing tickets with same hash within time window
3. If match found, mark as child/duplicate

### Configuration

```json
{
  "GerdaAI": {
    "SpamDetection": {
      "IsEnabled": true,
      "TimeWindowMinutes": 60,
      "MaxTicketsPerUser": 5,
      "Action": "AutoMerge",
      "GroupedTicketPrefix": "[GROUPED] "
    }
  }
}
```

### Key Features
- Zero-allocation duplicate check via indexed hash
- Configurable time window
- Auto-merge or flag-only modes

---

## E - Estimating (Sizer)

Assigns effort points based on ticket characteristics.

### Implementation

**Interface:** `IEstimatingService`  
**Location:** `Engine/GERDA/Estimating/`

**Algorithm:**
1. Parse ticket description for keywords
2. Match against category-complexity mapping
3. Assign Fibonacci points (1, 2, 3, 5, 8, 13)

### Configuration

```json
{
  "GerdaAI": {
    "ComplexityEstimation": {
      "IsEnabled": true,
      "CategoryComplexityMap": [
        { "Category": "Password Reset", "EffortPoints": 1 },
        { "Category": "Hardware Request", "EffortPoints": 5 },
        { "Category": "System Outage", "EffortPoints": 13 }
      ],
      "DefaultEffortPoints": 5
    }
  }
}
```

### Output
- `Ticket.EstimatedEffortPoints` - Fibonacci points (1-13)

---

## R - Ranking (Prioritizer)

Calculates priority scores using WSJF (Weighted Shortest Job First).

### Implementation

**Interface:** `IRankingService`  
**Location:** `Engine/GERDA/Ranking/`

**Algorithm (WSJF):**
```
Priority = (Cost of Delay × SLA Weight) / (Job Size × Complexity Weight)

Cost of Delay factors:
- Business value
- Time criticality (days until SLA breach)
- Risk reduction
```

### Configuration

```json
{
  "GerdaAI": {
    "Ranking": {
      "IsEnabled": true,
      "SlaWeight": 100,
      "ComplexityWeight": 1,
      "RecalculationFrequencyMinutes": 1440
    }
  }
}
```

### YAML Multipliers (Domain Config)

```yaml
ranking:
  base_formula: "cost_of_delay / job_size"
  multipliers:
    - condition: "days_until_breach <= 0"
      value: 10.0
    - condition: "days_until_breach <= 1"
      value: 5.0
    - condition: "customer_tier == 'VIP'"
      value: 2.0
```

### Output
- `Ticket.PriorityScore` - Double value for sorting

---

## D - Dispatching (Matchmaker)

Recommends the best agent for each ticket.

### Implementation

**Interface:** `IDispatchingService`  
**Location:** `Engine/GERDA/Dispatching/`

**Strategies:**
- `MatrixFactorization` - ML-based collaborative filtering
- `ZoneBased` - Geographic assignment
- `ExpertiseMatch` - Skill matching via FTS5

### Algorithm
```
Match Score = (ML_Score × 0.4) +
              (Expertise_Match × 0.3) +
              (Language_Match × 0.2) +
              (Geo_Match × 0.1) -
              (Capacity_Penalty × 0.5)
```

### Configuration

```json
{
  "GerdaAI": {
    "Dispatching": {
      "IsEnabled": true,
      "MinHistoryForAffinityMatch": 3,
      "MaxAssignedTicketsPerAgent": 15,
      "RetrainRecommendationModelFrequencyHours": 24
    }
  }
}
```

### YAML Weights (Domain Config)

```yaml
dispatching:
  weights:
    ml_score: 0.4
    expertise_match: 0.3
    language_match: 0.2
    geo_match: 0.1
  constraints:
    max_capacity_penalty: 0.5
```

### Output
- `Ticket.RecommendedProjectName` - Suggested agent name
- `Ticket.GerdaTags` - AI-Dispatched tag

---

## A - Anticipation (Forecaster)

Predicts future ticket volume and capacity risks.

### Implementation

**Interface:** `IAnticipationService`  
**Location:** `Engine/GERDA/Anticipation/`

**Algorithm:**
- ML.NET SSA (Singular Spectrum Analysis)
- Analyzes historical inflow patterns
- Detects seasonal trends

### Configuration

```json
{
  "GerdaAI": {
    "Anticipation": {
      "IsEnabled": true,
      "ForecastHorizonDays": 30,
      "InflowHistoryYears": 3,
      "MinHistoryForForecasting": 90,
      "CapacityRefreshFrequencyHours": 12,
      "RiskThresholdPercentage": 20
    }
  }
}
```

### Alerts

When predicted volume exceeds available capacity:
```
GERDA-A: Capacity risk detected!
Expected 150 tickets next week, only 120 agent-hours available.
Risk: 25%
```

---

## Background Processing

### GerdaBackgroundJob

Runs periodic GERDA processing on all open tickets.

**Registration:**
```csharp
services.AddHostedService<GerdaBackgroundJobService>();
```

**Processing:**
1. Fetches all open tickets
2. Runs full GERDA pipeline on each
3. Checks capacity risk (Anticipation)

### Queue-Based Processing

New tickets are processed via background queue:

```csharp
await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
{
    await gerdaService.ProcessTicketAsync(ticketGuid);
});
```

---

## Feature Extraction

The `DynamicFeatureExtractor` converts tickets to feature vectors.

```csharp
public interface IFeatureExtractor
{
    float[] ExtractFeatures(Ticket ticket, GerdaModelConfig config);
}
```

### Feature Types

| Type | Example | Transformation |
|------|---------|----------------|
| Numeric | `effort_points: 5` | Min-max normalization |
| Categorical | `type: "INCIDENT"` | One-hot encoding |
| Boolean | `is_vip: true` | 0/1 |
| Text | `description` | TF-IDF / embedding |

---

## Module Directory Structure

```
Engine/GERDA/
├── GerdaService.cs         # Main orchestrator
├── IGerdaService.cs        # Interface
├── NoOpGerdaService.cs     # Disabled mode
├── Configuration/          # Config models
├── Models/                 # Shared types
├── Grouping/               # G module
│   ├── IGroupingService.cs
│   └── GroupingService.cs
├── Estimating/             # E module
│   ├── IEstimatingService.cs
│   └── EstimatingService.cs
├── Ranking/                # R module
│   ├── IRankingService.cs
│   ├── WsjfRankingService.cs
│   └── Strategies/
├── Dispatching/            # D module
│   ├── IDispatchingService.cs
│   ├── DispatchingService.cs
│   └── Strategies/
├── Anticipation/           # A module
│   ├── IAnticipationService.cs
│   └── AnticipationService.cs
├── Strategies/             # Shared strategies
├── Features/               # Feature extraction
├── Tickets/                # Ticket-specific services
├── BackgroundJobs/         # Scheduled tasks
├── Persistence/            # Model storage
└── Explainability/         # AI decision explanations
```

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| In-process ML.NET | GDPR privacy, no API costs |
| Configuration-driven | Change behavior without redeploy |
| Background processing | Don't block UI threads |
| Optional services | Graceful degradation if disabled |
| Content hashing | O(1) duplicate detection |

---

## Disabling GERDA

Set in `masala_config.json`:

```json
{
  "GerdaAI": {
    "IsEnabled": false
  }
}
```

When disabled, `NoOpGerdaService` is used, returning immediately.

---

## Further Reading

- [Configuration Guide](../../guides/CONFIGURATION.md) - GERDA settings
- [Architecture Overview](../SUMMARY.md) - System design
- [Observer Pattern](../OBSERVERS.md) - Event-driven processing
