# GERDA: Complete Implementation Guide

**Project:** Ticket Masala - GovTech Extended Resource Dispatch & Anticipation  
**Version:** 2.0 (Consolidated)  
**Last Updated:** December 3, 2024

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [The Problem & Solution](#the-problem--solution)
3. [GERDA Architecture](#gerda-architecture)
4. [The Five AI Modules](#the-five-ai-modules)
5. [Technical Implementation](#technical-implementation)
6. [Configuration Guide](#configuration-guide)
7. [Implementation Status](#implementation-status)
8. [Next Steps](#next-steps)

---

## Executive Summary

GERDA is an AI-powered operations cockpit that transforms manual ticket distribution into an industrialized, data-driven dispatch system. It sits as an "operational overlay" on top of legacy systems (like SAP) to provide modern work management capabilities without replacing core infrastructure.

### Core Capabilities

- **Automatic grouping** of duplicate/related tickets
- **Complexity estimation** using Fibonacci points (1, 3, 5, 8, 13)
- **Priority ranking** with WSJF algorithm (Weighted Shortest Job First)
- **Intelligent dispatching** with multi-factor agent recommendations
- **Capacity forecasting** with 30-90 day predictions

### Current Status

| Component | Status | Technology |
|-----------|--------|------------|
| G - Grouping | ✅ **Implemented** | Rule-based LINQ |
| E - Estimating | ✅ **Implemented** | Category lookup + keyword matching |
| R - Ranking | ✅ **Implemented** | WSJF algorithm |
| D - Dispatching | ✅ **Implemented** | ML.NET Matrix Factorization |
| A - Anticipation | ✅ **Implemented** | ML.NET Time Series SSA |

---

## The Problem & Solution

### Universal Ticketing Challenges

| Challenge | Impact | GERDA Solution |
|-----------|--------|----------------|
| Manual Assignment | Hours of coordinator time | Automated recommendations |
| No Prioritization | FIFO or cherry-picking | WSJF priority scoring |
| Lost Knowledge | Agent turnover loses expertise | Affinity-based matching |
| Reactive Planning | Backlogs discovered too late | 90-day forecasting |
| Overloaded Agents | Burnout and inefficiency | Capacity-aware dispatch |

### Design Philosophy: Digital Twin Operations Cockpit

GERDA provides:

- **Work visibility:** Real-time backlog, agent capacity, SLA status
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

## GERDA Architecture

### Technology Stack

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| **Backend** | .NET 10 (C#) | Cross-platform, enterprise-grade |
| **Database** | SQL Server / PostgreSQL / SQLite | Entity Framework ORM |
| **AI/ML** | ML.NET | Native .NET ML framework |
| **Frontend** | ASP.NET MVC + Blazor | Server-side rendering |
| **Configuration** | JSON files | White-label design |

### Core Data Model

```csharp
public class Ticket : BaseModel
{
    public Status TicketStatus { get; set; } = Status.Pending;
    public string Description { get; set; }
    public string? CustomerId { get; set; }
    public string? ResponsibleId { get; set; }
    public Guid? ProjectGuid { get; set; }
    
    // GERDA AI Fields
    public int EstimatedEffortPoints { get; set; } = 0;
    public double PriorityScore { get; set; } = 0.0;
    public string? GerdaTags { get; set; }
}

public class Employee : ApplicationUser
{
    public string Team { get; set; }
    public EmployeeType Level { get; set; }
    
    // GERDA Extensions
    public string? Language { get; set; }
    public string? Specializations { get; set; }  // JSON array
    public int MaxCapacityPoints { get; set; } = 40;
    public string? Region { get; set; }
}
```

---

## The Five AI Modules

### 1. G — Grouping (The Noise Filter)

**Problem:** Repetitive requests from same customer clog queues

**Solution:** Rule-based clustering detects duplicate/related tickets and bundles them

**Algorithm:**
```csharp
// Time window-based grouping
var tickets = await _context.Tickets
    .Where(t => t.CustomerId == customerId)
    .Where(t => t.CreationDate >= DateTime.UtcNow.AddMinutes(-timeWindowMinutes))
    .ToListAsync();

if (tickets.Count > maxTicketsPerUser)
{
    // Create parent-child relationship
    var parentTicket = tickets.OrderBy(t => t.CreationDate).First();
    foreach (var childTicket in tickets.Skip(1))
    {
        childTicket.ParentTicketGuid = parentTicket.Guid;
    }
}
```

**Configuration:**
```json
{
  "SpamDetection": {
    "IsEnabled": true,
    "TimeWindowMinutes": 60,
    "MaxTicketsPerUser": 5,
    "Action": "AutoMerge"
  }
}
```

**Output:** Reduced noise; agents handle consolidated tickets

---

### 2. E — Estimating (The Sizer)

**Problem:** Not all tasks are equal in complexity

**Solution:** Category-based lookup maps tickets to Fibonacci points

**Algorithm:**
```csharp
private string ExtractCategory(Ticket ticket)
{
    var text = ticket.Description.ToLowerInvariant();
    
    // Keyword matching
    if (text.Contains("password")) return "Password Reset";
    if (text.Contains("hardware")) return "Hardware Request";
    if (text.Contains("bug")) return "Software Bug";
    if (text.Contains("outage")) return "System Outage";
    
    return "Other";
}

public int GetComplexityByCategory(string category)
{
    return _complexityMap.GetValueOrDefault(category, defaultEffortPoints);
}
```

**Complexity Mapping:**

| Category | Effort Points | Complexity Level |
|----------|--------------|------------------|
| Password Reset | 1 | Trivial |
| Address Change | 1 | Trivial |
| Hardware Request | 3 | Simple |
| Software Bug | 8 | Medium |
| System Outage | 13 | Complex |
| Fraud Investigation | 13 | Complex |
| Other | 5 | Medium (default) |

**Configuration:**
```json
{
  "ComplexityEstimation": {
    "IsEnabled": true,
    "CategoryComplexityMap": [
      { "Category": "Password Reset", "EffortPoints": 1 },
      { "Category": "Hardware Request", "EffortPoints": 3 },
      { "Category": "Software Bug", "EffortPoints": 8 },
      { "Category": "System Outage", "EffortPoints": 13 }
    ],
    "DefaultEffortPoints": 5
  }
}
```

**Output:** Every ticket receives a complexity score (Fibonacci scale: 1, 2, 3, 5, 8, 13, 21)

---

### 3. R — Ranking (The Prioritizer)

**Problem:** Agents cherry-pick easy tasks; urgent tickets breach SLAs

**Solution:** WSJF algorithm (Weighted Shortest Job First)

**Formula:**

$$\text{Priority Score} = \frac{\text{Cost of Delay}}{\text{Job Size (Fibonacci Points)}}$$

**Algorithm:**
```csharp
private double CalculateCostOfDelay(Ticket ticket)
{
    var daysUntilBreach = (ticket.CompletionTarget - DateTime.UtcNow).TotalDays;
    
    // Category-based urgency multiplier
    var urgencyMultiplier = _config.Queues
        .SelectMany(q => q.UrgencyMultipliers)
        .GetValueOrDefault(ticket.Category, 1.0);
    
    // Urgency increases as deadline approaches
    if (daysUntilBreach <= 0) return urgencyMultiplier * 10.0;  // Breached!
    if (daysUntilBreach <= 1) return urgencyMultiplier * 5.0;   // Critical
    if (daysUntilBreach <= 3) return urgencyMultiplier * 2.0;   // High
    
    return urgencyMultiplier / Math.Max(daysUntilBreach, 1);
}

public async Task<double> CalculatePriorityAsync(Guid ticketGuid)
{
    var ticket = await _context.Tickets.FindAsync(ticketGuid);
    var costOfDelay = CalculateCostOfDelay(ticket);
    var jobSize = Math.Max(ticket.EstimatedEffortPoints, 1);
    
    return costOfDelay / jobSize;
}
```

**Example:**
```
Ticket A: Support request, 2 days until breach, Size = 1 point
  → Cost of Delay = 2.0 / 2 = 1.0
  → Priority = 1.0 / 1 = 1.0

Ticket B: Feature request, 10 days until breach, Size = 8 points
  → Cost of Delay = 1.0 / 10 = 0.1
  → Priority = 0.1 / 8 = 0.0125

Result: Ticket A gets higher priority (quick win, urgent)
```

**Configuration:**
```json
{
  "Ranking": {
    "IsEnabled": true,
    "SlaWeight": 100,
    "ComplexityWeight": 1,
    "RecalculationFrequencyMinutes": 1440
  }
}
```

**Output:** Dynamically ranked work queue maximizing value per agent hour

---

### 4. D — Dispatching (The Matchmaker)

**Problem:** New agents lack context; experienced agents overloaded

**Solution:** ML.NET Matrix Factorization for agent-ticket matching

**Multi-Factor Affinity Scoring:**

```csharp
private double CalculateAffinityScore(Employee agent, Ticket ticket, double mlScore)
{
    // 4-factor weighted scoring
    var pastInteraction = mlScore * 0.4;              // 40% - ML prediction
    
    var specializations = JsonSerializer.Deserialize<List<string>>(
        agent.Specializations ?? "[]");
    var expertiseMatch = specializations.Contains(ticket.Category) ? 0.3 : 0.1;
    
    var languageMatch = (agent.Language == ticket.Customer?.Language) ? 0.2 : 0.1;
    
    var geoMatch = (agent.Region == ticket.Customer?.Region) ? 0.1 : 0.05;
    
    return pastInteraction + expertiseMatch + languageMatch + geoMatch;
}
```

**Capacity Constraint:**
```csharp
public async Task<string?> GetRecommendedAgentAsync(Guid ticketGuid)
{
    var ticket = await _context.Tickets
        .Include(t => t.Customer)
        .FirstOrDefaultAsync(t => t.Guid == ticketGuid);
    
    // Get agents with available capacity
    var availableAgents = await _context.Users.OfType<Employee>()
        .Where(e => e.CurrentWorkload < e.MaxCapacityPoints)
        .ToListAsync();
    
    // Score each agent
    var recommendations = new List<(Employee agent, double score)>();
    foreach (var agent in availableAgents)
    {
        var mlScore = await PredictAffinityAsync(agent.Id, ticket.CustomerId);
        var totalScore = CalculateAffinityScore(agent, ticket, mlScore);
        recommendations.Add((agent, totalScore));
    }
    
    // Return best match
    return recommendations
        .OrderByDescending(r => r.score)
        .FirstOrDefault()
        .agent?.Id;
}
```

**ML Model Training:**
```csharp
public async Task RetrainModelAsync()
{
    // Build training data from historical assignments
    var trainingData = await _context.Tickets
        .Where(t => t.ResponsibleId != null && t.CustomerId != null)
        .Where(t => t.TicketStatus == Status.Completed)
        .Select(t => new AgentCustomerRating
        {
            AgentId = t.ResponsibleId,
            CustomerId = t.CustomerId,
            Rating = CalculateImplicitRating(t)  // Based on resolution time
        })
        .ToListAsync();
    
    // Train Matrix Factorization model
    var pipeline = _mlContext.Transforms.Conversion
        .MapValueToKey("AgentIdEncoded", nameof(AgentCustomerRating.AgentId))
        .Append(_mlContext.Transforms.Conversion
            .MapValueToKey("CustomerIdEncoded", nameof(AgentCustomerRating.CustomerId)))
        .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(
            labelColumnName: nameof(AgentCustomerRating.Rating),
            matrixColumnIndexColumnName: "AgentIdEncoded",
            matrixRowIndexColumnName: "CustomerIdEncoded",
            numberOfIterations: 20,
            approximationRank: 100));
    
    _model = pipeline.Fit(trainingDataView);
}
```

**Configuration:**
```json
{
  "Dispatching": {
    "IsEnabled": true,
    "MinHistoryForAffinityMatch": 3,
    "MaxAssignedTicketsPerAgent": 15,
    "RetrainRecommendationModelFrequencyHours": 24
  }
}
```

**Output:** Optimal agent-ticket pairing with capacity balancing

---

### 5. A — Anticipation (The Weather Report)

**Problem:** Teams react to backlogs instead of preventing them

**Solution:** ML.NET Time Series Forecasting (SSA)

**Algorithm:**
```csharp
public async Task<CapacityRisk> CheckCapacityRiskAsync()
{
    // Historical inflow data
    var historicalData = await _context.Tickets
        .Where(t => t.CreationDate >= DateTime.UtcNow.AddYears(-3))
        .GroupBy(t => t.CreationDate.Date)
        .Select(g => new TicketInflowData
        {
            Date = g.Key,
            TicketCount = g.Count()
        })
        .OrderBy(d => d.Date)
        .ToListAsync();
    
    // Train SSA model
    var dataView = _mlContext.Data.LoadFromEnumerable(historicalData);
    var pipeline = _mlContext.Forecasting.ForecastBySsa(
        outputColumnName: nameof(TicketInflowForecast.ForecastedTickets),
        inputColumnName: nameof(TicketInflowData.TicketCount),
        windowSize: 30,
        seriesLength: 90,
        trainSize: historicalData.Count,
        horizon: 30);
    
    var model = pipeline.Fit(dataView);
    
    // Forecast next 30 days
    var forecastEngine = model.CreateTimeSeriesEngine<TicketInflowData, TicketInflowForecast>(
        _mlContext);
    var forecast = forecastEngine.Predict();
    
    // Calculate team capacity
    var teamCapacity = await CalculateTeamCapacityAsync();
    
    // Check for bottlenecks
    var predictedInflow = forecast.ForecastedTickets.Sum();
    var riskPercentage = (predictedInflow - teamCapacity) / teamCapacity;
    
    return new CapacityRisk
    {
        PredictedInflow = predictedInflow,
        TeamCapacity = teamCapacity,
        RiskPercentage = riskPercentage,
        AlertLevel = riskPercentage > 0.3 ? "Critical" : 
                     riskPercentage > 0.15 ? "Warning" : "Normal"
    };
}
```

**Configuration:**
```json
{
  "Anticipation": {
    "IsEnabled": true,
    "ForecastHorizonDays": 30,
    "InflowHistoryYears": 3,
    "MinHistoryForForecasting": 90,
    "CapacityRefreshFrequencyHours": 12,
    "RiskThresholdPercentage": 20
  }
}
```

**Output:** Director dashboard with early-warning alerts for capacity planning

---

## Technical Implementation

### Service Registration

```csharp
// Program.cs
builder.Services.AddScoped<IGroupingService, GroupingService>();
builder.Services.AddScoped<IEstimatingService, EstimatingService>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddScoped<IDispatchingService, DispatchingService>();
builder.Services.AddScoped<IAnticipationService, AnticipationService>();
builder.Services.AddScoped<IGerdaService, GerdaService>();

// Background services
builder.Services.AddHostedService<GerdaBackgroundService>();

// Configuration
builder.Services.AddSingleton(sp =>
{
    var config = new ConfigurationBuilder()
        .AddJsonFile("masala_config.json", optional: false)
        .Build();
    return config.Get<GerdaConfig>();
});
```

### Using GERDA in Controllers

```csharp
[HttpPost]
public async Task<IActionResult> Create(NewTicketViewModel model)
{
    var ticket = MapViewModelToTicket(model);
    await _context.Tickets.AddAsync(ticket);
    await _context.SaveChangesAsync();
    
    // GERDA Integration
    if (_gerdaService.IsEnabled)
    {
        await _gerdaService.ProcessTicketAsync(ticket.Guid);
        TempData["Success"] = "Ticket created and processed by GERDA AI";
    }
    
    return RedirectToAction("Details", new { id = ticket.Guid });
}
```

### Background Processing

```csharp
public class GerdaBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Recalculate priorities every 6 hours
            await _rankingService.RecalculateAllPrioritiesAsync();
            
            // Retrain dispatching model daily at 2 AM
            if (DateTime.Now.Hour == 2)
            {
                await _dispatchingService.RetrainModelAsync();
            }
            
            await Task.Delay(TimeSpan.FromHours(6), ct);
        }
    }
}
```

---

## Configuration Guide

### Master Configuration: `masala_config.json`

```json
{
  "AppInstanceName": "Ticket Masala - GERDA AI",
  "AppDescription": "Intelligent ticketing and case management system",
  "DefaultSlaThresholdDays": 30,
  
  "Queues": [
    {
      "Name": "Customer Support (IT)",
      "Code": "ITCS",
      "Description": "General IT support requests",
      "IsActive": true,
      "AutoArchiveDays": 180,
      "SlaDefaults": {
        "Password Reset": 1,
        "Hardware Request": 3,
        "Software Bug": 7,
        "System Outage": 1,
        "Other": 5
      },
      "UrgencyMultipliers": {
        "Password Reset": 1.5,
        "Hardware Request": 2.0,
        "Software Bug": 1.8,
        "System Outage": 5.0,
        "Other": 1.0
      }
    }
  ],
  
  "GerdaAI": {
    "IsEnabled": true,
    
    "SpamDetection": {
      "IsEnabled": true,
      "TimeWindowMinutes": 60,
      "MaxTicketsPerUser": 5,
      "Action": "AutoMerge",
      "GroupedTicketPrefix": "[GROUPED] "
    },
    
    "ComplexityEstimation": {
      "IsEnabled": true,
      "CategoryComplexityMap": [
        { "Category": "Password Reset", "EffortPoints": 1 },
        { "Category": "Hardware Request", "EffortPoints": 3 },
        { "Category": "Software Bug", "EffortPoints": 8 },
        { "Category": "System Outage", "EffortPoints": 13 },
        { "Category": "Other", "EffortPoints": 5 }
      ],
      "DefaultEffortPoints": 5
    },
    
    "Ranking": {
      "IsEnabled": true,
      "SlaWeight": 100,
      "ComplexityWeight": 1,
      "RecalculationFrequencyMinutes": 1440
    },
    
    "Dispatching": {
      "IsEnabled": true,
      "MinHistoryForAffinityMatch": 3,
      "MaxAssignedTicketsPerAgent": 15,
      "RetrainRecommendationModelFrequencyHours": 24
    },
    
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

---

## Implementation Status

### ✅ Completed Features

- **All 5 GERDA services implemented** (G+E+R+D+A)
- Database schema with GERDA fields (`EstimatedEffortPoints`, `PriorityScore`, `GerdaTags`)
- ML.NET Matrix Factorization for agent recommendations
- ML.NET Time Series SSA for capacity forecasting
- WSJF priority calculation
- Background service for automatic processing
- Configuration-driven behavior
- Dependency injection setup

### 🚧 In Progress

- UI components for GERDA insights
- Manager dashboard with team metrics
- Agent queue view with recommendations
- File watcher for legacy system integration

### 📋 Planned

- Performance optimization (caching, indexing)
- Metrics tracking dashboard
- User documentation
- Production deployment

---

## Next Steps

### Immediate (This Week)

1. ✅ Test GERDA Dispatch Dashboard
2. ✅ Create sample unassigned tickets
3. ⏳ Verify ML model training with sufficient data
4. ⏳ Test batch assignment functionality

### Short Term (Next 2 Weeks)

1. Add team capacity visualization
2. Implement workload balancing alerts
3. Create agent performance metrics
4. Add export functionality for reports

### Long Term (Next Month)

1. API integration with legacy systems
2. Mobile-responsive UI improvements
3. Advanced forecasting features
4. Multi-language support

---

## Success Metrics

### Operational KPIs

| Metric | Baseline (Manual) | Target (GERDA) | Current |
|--------|-------------------|----------------|---------|
| Time to First Action | 3-5 days | <1 day | TBD |
| SLA Breach Rate | 10-20% | <5% | TBD |
| Agent Workload Balance | High variance | Low variance | TBD |
| Manager Assignment Time | 5-10 hours/week | <1 hour/week | TBD |

### Quality KPIs

| Metric | Target | Current |
|--------|--------|---------|
| Recommendation Acceptance Rate | >80% | TBD |
| Forecast Accuracy (MAE) | <15% error | TBD |
| False Positive Grouping Rate | <5% | TBD |

---

**Document Version:** 2.0  
**Last Updated:** December 3, 2024  
**Next Review:** After first production deployment
