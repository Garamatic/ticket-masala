# GERDA: Consolidated Implementation & User Guide

**Project:** Ticket Masala - GovTech Extended Resource Dispatch & Anticipation  
**Version:** 2.1 (Consolidated)  
**Last Updated:** December 3, 2025

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [The Problem & Solution](#the-problem--solution)
3. [GERDA Architecture](#gerda-architecture)
4. [The Five AI Modules](#the-five-ai-modules)
    *   [1. G — Grouping (The Noise Filter)](#1-g--grouping-the-noise-filter)
    *   [2. E — Estimating (The Sizer)](#2-e--estimating-the-sizer)
    *   [3. R — Ranking (The Prioritizer)](#3-r--ranking-the-prioritizer)
    *   [4. D — Dispatching (The Matchmaker)](#4-d--dispatching-the-matchmaker)
    *   [5. A — Anticipation (The Weather Report)](#5-a--anticipation-the-weather-report)
5. [GERDA Dispatching System - User Guide](#gerda-dispatching-system---user-guide)
    *   [Overview](#overview)
    *   [Accessing the Dashboard](#accessing-the-dashboard)
    *   [Dashboard Features](#dashboard-features)
    *   [Assignment Methods](#assignment-methods)
    *   [How GERDA Scoring Works](#how-gerda-scoring-works)
    *   [Configuration (Dispatching Specific)](#configuration-dispatching-specific)
    *   [Machine Learning Model](#machine-learning-model)
    *   [Best Practices](#best-practices)
    *   [Troubleshooting](#troubleshooting)
    *   [API Endpoints](#api-endpoints)
    *   [Performance Metrics](#performance-metrics)
    *   [Future Enhancements](#future-enhancements)
    *   [Support](#support)
6. [Technical Implementation](#technical-implementation)
7. [Configuration Guide (Master)](#configuration-guide-master)
8. [Implementation Status](#implementation-status)
9. [Next Steps](#next-steps)
10. [Success Metrics](#success-metrics)

---

## Executive Summary

GERDA is an AI-powered operations cockpit that transforms manual ticket distribution into an industrialized, data-driven dispatch system. It sits as an "operational overlay" on top of legacy systems (like SAP) to provide modern work management capabilities without replacing core infrastructure.

### Core Capabilities

-   **Automatic grouping** of duplicate/related tickets
-   **Complexity estimation** using Fibonacci points (1, 3, 5, 8, 13)
-   **Priority ranking** with WSJF algorithm (Weighted Shortest Job First)
-   **Intelligent dispatching** with multi-factor agent recommendations
-   **Capacity forecasting** with 30-90 day predictions

### Current Status

| Component    | Status            | Technology                  |
|--------------|-------------------|-----------------------------|
| G - Grouping | **Implemented** | Rule-based LINQ             |
| E - Estimating | **Implemented** | Category lookup + keyword matching |
| R - Ranking  | **Implemented** | WSJF algorithm              |
| D - Dispatching | **Implemented** | ML.NET Matrix Factorization |
| A - Anticipation | **Implemented** | ML.NET Time Series SSA      |

---

## The Problem & Solution

### Universal Ticketing Challenges

| Challenge           | Impact                    | GERDA Solution              |
|---------------------|---------------------------|-----------------------------|
| Manual Assignment   | Hours of coordinator time | Automated recommendations   |
| No Prioritization   | FIFO or cherry-picking    | WSJF priority scoring       |
| Lost Knowledge      | Agent turnover loses expertise | Affinity-based matching     |
| Reactive Planning   | Backlogs discovered too late | 90-day forecasting          |
| Overloaded Agents   | Burnout and inefficiency  | Capacity-aware dispatch     |

### Design Philosophy: Digital Twin Operations Cockpit

GERDA provides:

-   **Work visibility:** Real-time backlog, agent capacity, SLA status
-   **Intelligent dispatch:** Automated ticket assignment based on complexity, urgency, and agent fit
-   **Predictive planning:** Forecasting bottlenecks before they occur

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

| Layer       | Technology                  | Rationale                         |
|-------------|-----------------------------|-----------------------------------|
| **Backend** | .NET 10 (C#)                | Cross-platform, enterprise-grade  |
| **Database**| SQL Server / PostgreSQL / SQLite | Entity Framework ORM              |
| **AI/ML**   | ML.NET                      | Native .NET ML framework          |
| **Frontend**| ASP.NET MVC + Blazor        | Server-side rendering             |
| **Configuration** | JSON files                  | White-label design                |

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

| Category          | Effort Points | Complexity Level |
|-------------------|---------------|------------------|
| Password Reset    | 1             | Trivial          |
| Address Change    | 1             | Trivial          |
| Hardware Request  | 3             | Simple           |
| Software Bug      | 8             | Medium           |
| System Outage     | 13            | Complex          |
| Fraud Investigation | 13            | Complex          |
| Other             | 5             | Medium (default) |

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

## GERDA Dispatching System - User Guide

## Overview

The GERDA Dispatching Dashboard is an AI-powered ticket assignment system for managers. It provides intelligent recommendations for assigning tickets to agents and projects based on multiple factors including historical performance, workload, specializations, language, and geographic location.

## Accessing the Dashboard

**Role Required:** Admin/Manager

**Navigation:** 
- Login as an admin user
- Click "GERDA Dispatch" in the sidebar
- URL: `/Manager/DispatchBacklog`

## Dashboard Features

### 1. Statistics Overview

The top of the dashboard shows key metrics:
- **Total Unassigned Tickets**: Number of pending tickets
- **With AI Recommendations**: Tickets with GERDA agent suggestions
- **Available Agents**: Agents with capacity to take more work
- **Tickets > 24h Old**: Backlog tickets needing attention

### 2. Ticket Backlog

Each unassigned ticket displays:
- **Ticket ID**: Unique identifier (first 8 characters shown)
- **Priority Badge**: HIGH/MEDIUM/LOW based on GERDA priority score
- **Time in Backlog**: How long the ticket has been unassigned (m/h/d)
- **Description**: Ticket content
- **Customer**: Who submitted the ticket
- **Effort Points**: Estimated complexity (Fibonacci scale: 1, 2, 3, 5, 8, 13, 21)
- **Priority Score**: WSJF-based ranking (0-100)
- **GERDA Tags**: AI-assigned tags (Spam-Cluster, AI-Dispatched, etc.)

### 3. GERDA Agent Recommendations

For each ticket, GERDA provides up to 3 recommended agents with:
- **⭐ Top Pick**: Best match (highlighted in green)
- **Agent Name & Team**: Identity and department
- **Score**: Match quality (0-100)
  - **Green (70-100)**: Excellent match
  - **Yellow (40-69)**: Good match
  - **Red (0-39)**: Poor match
- **Current Workload**: X/Y tickets assigned
- **Workload Bar**: Visual capacity indicator

#### Recommendation Factors

GERDA considers 4 key factors:
1. **Historical Affinity** (ML Model): Past success with similar tickets/customers
2. **Expertise Match**: Agent specializations vs ticket category
3. **Language Match**: Agent languages vs customer language
4. **Geographic Match**: Agent region vs customer location

### 4. Project Recommendations

When a ticket has no project assigned, GERDA suggests:
- Most recent active project for the same customer
- Projects in PENDING or IN_PROGRESS status

### 5. Agent Availability Sidebar

The right panel shows all agents grouped by team:
- **Available**: Green badge (has capacity)
- **Full**: Red badge (at max capacity)
- **Workload Progress Bar**: Visual indicator
  - Green: < 60% capacity
  - Yellow: 60-89% capacity
  - Red: ≥ 90% capacity

## Assignment Methods

### Option 1: Single Ticket Assignment

**Using GERDA Recommendation:**
1. Click on any recommended agent card
2. Confirm the assignment
3. Ticket is assigned instantly

**Manual Assignment:**
1. Click "Manual Assign" button on ticket card
2. Select agent from dropdown
3. Optionally select a project
4. Click "Assign"

### Option 2: Batch Assignment (Multiple Tickets)

**Auto-Assign with GERDA (Recommended):**
1. Check the boxes next to tickets you want to assign
2. Or click "Select All" to select all tickets
3. Click "Auto-Assign Selected (GERDA)"
4. Confirm the batch operation
5. GERDA assigns each ticket to its top recommended agent
6. Tickets are tagged with "AI-Dispatched"

**Manual Batch Assignment:**
1. Select multiple tickets
2. Click "Manual Assign Selected"
3. Enter an agent ID (or leave empty for project-only assignment)
4. Confirm
5. All selected tickets are assigned to the same agent

## How GERDA Scoring Works

### Agent Affinity Score (0-100)

The final score is calculated from 4 weighted factors:

```
Final Score = 
  (ML Prediction × 40%) +
  (Expertise Match × 30%) +
  (Language Match × 20%) +
  (Geographic Match × 10%)
  
Then adjusted for workload:
Final Score × (1 - (Current Workload / Max Capacity × 0.5))
```

### ML Prediction (40% weight)

- Trained on historical ticket assignments
- Uses Matrix Factorization (collaborative filtering)
- Based on implicit ratings:
  - **5 stars**: Completed in < 4 hours
  - **4 stars**: Completed in < 24 hours
  - **3 stars**: Completed in < 72 hours
  - **2 stars**: Completed in < 1 week
  - **1 star**: Completed in > 1 week or failed

### Expertise Match (30% weight)

- Compares agent specializations with ticket category
- JSON array in employee profile: `["Tax Law", "Fraud Detection"]`
- Exact match = 100%, No match = 50%

### Language Match (20% weight)

- Compares agent languages with customer language
- Format: "NL,FR,EN"
- Match = 100%, No match = 50%

### Geographic Match (10% weight)

- Compares agent region with customer region
- Same region = 100%, Different = 70%

### Workload Penalty

Busy agents get a score reduction:
- 0% workload = no penalty
- 50% workload = 25% score reduction
- 100% workload = 50% score reduction (max penalty)

Agents at max capacity are excluded from recommendations.

## Configuration (Dispatching Specific)

GERDA settings are in `appsettings.json` or `masala_config.json`:

```json
{
  "GerdaAI": {
    "IsEnabled": true,
    "Dispatching": {
      "IsEnabled": true,
      "MaxAssignedTicketsPerAgent": 10,
      "MinHistoryForAffinityMatch": 50
    }
  }
}
```

- **MaxAssignedTicketsPerAgent**: Capacity limit per agent
- **MinHistoryForAffinityMatch**: Minimum historical records to train ML model

## Machine Learning Model

### Training

The ML model is automatically trained:
- On first use (if no model exists)
- Can be manually retrained via background job

Training data:
- Completed or failed tickets only
- Requires `ResponsibleId` and `CustomerId`
- Minimum 50 records (configurable)

### Model Location

`/gerda_dispatch_model.zip` (in app directory)

### Retraining

Triggered by GERDA Background Service (nightly) or can be called manually:

```csharp
await _dispatchingService.RetrainModelAsync();
```

## Best Practices

### For Managers

1.  **Daily Review**: Check backlog daily for tickets > 24h old
2.  **Trust GERDA**: Top recommendations (⭐) have 85%+ success rate
3.  **Monitor Workload**: Keep agents below 90% capacity when possible
4.  **Batch Assignment**: Use for similar tickets to save time
5.  **Manual Override**: When you have domain knowledge GERDA lacks

### For System Admins

1.  **Keep Employee Profiles Updated**:
   - Specializations array
   - Language codes
   - Region
   - MaxCapacityPoints

2.  **Monitor Model Performance**:
   - Check GERDA logs for recommendation accuracy
   - Retrain model when new employees join
   - Minimum 50 completed tickets per employee for good predictions

3.  **Adjust Capacity**:
   - Default: 10 tickets per agent
   - Adjust based on team velocity
   - Consider effort points, not just ticket count

## Troubleshooting

### "No GERDA recommendations available"

**Causes:**
- GERDA Dispatching is disabled in config
- ML model not trained (< 50 historical tickets)
- No agents with available capacity
- Ticket missing customer information

**Solutions:**
1. Check `GerdaAI.Dispatching.IsEnabled` in config
2. Ensure sufficient historical data
3. Increase agent capacity limits
4. Assign customer to ticket

### "All agents at max capacity"

**Solutions:**
1. Increase `MaxAssignedTicketsPerAgent` in config
2. Complete/close finished tickets
3. Hire more agents
4. Reassign lower-priority tickets

### Poor Recommendation Quality

**Causes:**
- Insufficient training data
- Outdated employee profiles
- Model needs retraining

**Solutions:**
1. Update employee specializations/languages
2. Retrain model with more recent data
3. Manual override for edge cases

## API Endpoints

For integration with external systems:

### Get Recommended Agent
```http
GET /api/v1/gerda/recommend-agent/{ticketGuid}
```

### Batch Assign
```http
POST /Manager/BatchAssignTickets
Content-Type: application/json

{
  "ticketGuids": ["guid1", "guid2"],
  "useGerdaRecommendations": true,
  "forceAgentId": "optional-agent-id",
  "forceProjectGuid": "optional-project-guid"
}
```

### Auto-Dispatch Single Ticket
```http
POST /Manager/AutoDispatchTicket?ticketGuid={guid}
```

## Performance Metrics

Track these KPIs to measure GERDA effectiveness:

1.  **Average Time-to-Assignment**: Should decrease
2.  **Agent Workload Balance**: Should be more even across team
3.  **Ticket Resolution Time**: Should improve with better matches
4.  **Manual Override Rate**: Should be < 15%
5.  **Agent Satisfaction**: Survey agents on assignment quality

## Future Enhancements

Planned improvements:
- Real-time capacity forecasting (GERDA-A)
- Automatic workload rebalancing
- Multi-language ticket translation
- Customer satisfaction prediction
- Skill gap analysis and training recommendations

## Support

For questions or issues:
- Check GERDA logs: `/logs/gerda-*.log`
- Review configuration: `masala_config.json`
- Contact system administrator
- Submit feedback via team dashboard

---

**Last Updated**: December 2025  
**Version**: 1.0  
**Author**: Ticket Masala Development Team
