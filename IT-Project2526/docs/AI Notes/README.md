# Project Name: G.E.R.D.A. (GovTech Extended Resource Dispatch & Anticipation)

## Mission
Move from "Passive Ticket Tracking" to "Active Operational Orchestration" within the "Ticket Masala" platform.

## The Problem: Legacy System Bottleneck

Many large organizations (governments, banks, insurance companies) rely on robust but cumbersome "Systems of Record" (e.g., SAP). While these systems are legally required and hold master data, they are inefficient for day-to-day operational management:

-   **The System of Record:** Legally required, slow, difficult to manage, lacks agility.
-   **The Need:** A fast, modern "cockpit" for team leaders to distribute work, track velocity, monitor SLAs, and identify bottlenecks in real-time.

## The Solution: Digital Twin Operations Cockpit

"Ticket Masala" acts as an **Operational Overlay** sitting on top of the legacy system, providing agility without replacing core infrastructure:

-   **.NET App (Ticket Masala):** Where work is actively _managed_, prioritized, and dispatched for operational efficiency.
-   **Legacy System (e.g., SAP):** Where the case officially _lives_ and is legally recorded.

This architecture is **Read-Heavy, Write-Light** with periodic one-way sync from the legacy system to the .NET app's database.

### Data Ingestion Strategies

1.  **The "Senior Architect" Way (API Integration):** Direct connection to legacy APIs (e.g., SAP .NET Connector, BAPIs) - most robust and professional.
2.  **The "Hacker" Way (File Drop):** Manager exports case list to spreadsheet, .NET app monitors shared folder with file watcher - requires zero IT permissions, quick proof of value.

## High-Level Architecture: "Ticket Masala" (White Label Ticketing Engine)

"Ticket Masala" is designed as a generic, configurable ticketing system that can adapt to various organizational needs (e.g., IT Helpdesk, Tax Office). Its core principles are:

-   **Modular Architecture:** Clear separation of concerns between the core ticketing logic and the intelligent "GERDA" services.
-   **Configuration over Code:** Behaviors, roles, and categories are defined via `masala_config.json`, allowing for flexible deployment without code changes.
-   **Role-Based Access Control (RBAC):** Supports a flexible hierarchy of users (Admin, Employee, Customer).
-   **Project → Ticket Hierarchy:** "Projects" act as containers for work (workstreams/queues, tax years, regional directorates), not traditional software projects.
-   **Mock Data Driven:** Utilizes synthetic data generation (Bogus library) to simulate real-world operational patterns for AI training and demonstration, preserving data privacy.

### Core Components:

1.  **Ticket Masala Application:**
    -   .NET MVC/Core application.
    -   Entity Framework for data persistence.
    -   Generic CRUD operations for `Projects` (Queues/Workstreams), `Tickets` (Cases), and `Users`.
    -   API endpoints for external integration and UI interaction.
2.  **masala_config.json:** The central configuration file defining application instance name, GERDA settings, queue definitions, SLA thresholds, etc.
3.  **Mock Data Generator:** A data seeder that populates the database with statistically realistic synthetic data for various scenarios (e.g., IT Support, Tax Office) to enable GERDA's training and demonstration.

## GERDA: The Intelligent Orchestration Engine

GERDA is an internal service within the "Ticket Masala" platform, leveraging ML.NET to provide intelligent automation and insights. Its functionality is configured via `masala_config.json`.

---

### 1. G - Grouping (The Noise Filter)

-   **Business Problem:** "Spammy" clients or repetitive requests clog the backlog, leading to redundant work.
-   **Current Implementation:** **Rule-based grouping** (no ML) - detects when the same customer submits multiple tickets within a time window and auto-groups them.
-   **Tech Stack:** LINQ queries with time-window filtering (configured via `masala_config.json`).
-   **Logic:** Counts tickets from same `CustomerId` within `TimeWindowMinutes`. If count exceeds `MaxTicketsPerUser`, creates parent-child ticket relationship.
-   **Action:** Auto-bundles multiple related tickets into one "Parent Ticket" to streamline processing and reduce agent workload.
-   **Future Enhancement:** ML.NET **K-Means Clustering** to detect similarity beyond just same customer (e.g., similar descriptions, same issue category).

#### Configuration Example (`masala_config.json`)

```json
"SpamDetection": {
  "IsEnabled": true,
  "TimeWindowMinutes": 60,
  "MaxTicketsPerUser": 5,      // If > 5 tickets from same user in 1 hour -> GROUP THEM
  "Action": "AutoMerge",        // or "Flag" for manual review
  "GroupedTicketPrefix": "[GROUPED] "
}
```

---

### 2. E - Estimating (The Sizer)

-   **Business Problem:** Treating all tasks equally, regardless of actual effort, leads to inefficient queue management.
-   **Current Implementation:** **Category lookup table** (no ML) - maps ticket categories to Fibonacci complexity points via configuration.
-   **Tech Stack:** Dictionary-based keyword matching using `CategoryComplexityMap` from `masala_config.json`.
-   **Logic:** Extracts category from ticket description/project name using keyword matching, then looks up pre-configured effort points (1, 3, 5, 8, 13).
-   **Action:** Assigns `EstimatedEffortPoints` to ticket for use in WSJF ranking algorithm.
-   **Future Enhancement:** ML.NET **Multi-Class Classification** trained on historical cycle times to predict complexity dynamically instead of static lookup.

#### Configuration Example (`masala_config.json`)

```json
"ComplexityEstimation": {
  "IsEnabled": true,
  "CategoryComplexityMap": [
    { "Category": "Password Reset", "EffortPoints": 1 },      // S (Tax equivalent: Address Change)
    { "Category": "Hardware Request", "EffortPoints": 3 },    // M
    { "Category": "Software Bug", "EffortPoints": 8 },        // L
    { "Category": "System Outage", "EffortPoints": 13 }       // XL (Tax equivalent: Audit)
  ],
  "DefaultEffortPoints": 5
}
```

---

### 3. R - Ranking (The Prioritizer)

-   **Business Problem:** Agents often cherry-pick easy tasks, causing urgent/hard tasks to breach SLAs.
-   **Current Implementation:** **Interface defined, not yet implemented.**
-   **Tech Stack:** **WSJF Algorithm** (Weighted Shortest Job First) - algorithmic, no ML required.
-   **Logic:** Calculates a `PriorityScore` dynamically for each ticket using the formula:

$$Priority = \frac{\text{Cost of Delay (SLA Breach Risk)}} {\text{Job Size (Fibonacci Points)}}$$

-   `Cost of Delay`: Derived from `SlaWeight` config and the ticket's age relative to completion target.
-   `Job Size`: Obtained from the Estimating (E) step (`EstimatedEffortPoints`).

-   **Action:** Re-orders the queue dynamically to prioritize high-value, low-effort tasks and prevent SLA breaches.

#### Configuration Example (`masala_config.json`)

```json
"Ranking": {
  "IsEnabled": true,
  "SlaWeight": 100,                        // Factor to emphasize SLA urgency
  "ComplexityWeight": 1,                   // Factor for job size (usually 1)
  "RecalculationFrequencyMinutes": 1440    // Daily recalculation
}
```

---

### 4. D - Dispatching (The Matchmaker)

-   **Business Problem:** Loss of institutional knowledge when new agents handle clients with complex history; inefficient assignment to overloaded agents.
-   **Current Implementation:** **Interface defined, not yet implemented.**
-   **Tech Stack:** ML.NET **Matrix Factorization (Recommendation System)**.
-   **Logic:** 
    - Builds `[AgentId, CustomerId, Rating]` matrix from historical ticket assignments and resolution quality
    - Uses collaborative filtering to predict affinity scores for unseen agent-customer pairs
    - Filters recommendations by agent availability and current workload (`MaxAssignedTicketsPerAgent`)
-   **Action:** Recommends the most suitable agent for the highest-priority cases, balancing affinity with workload.
-   **ML Model:** Trained on historical `Ticket.ResponsibleId` and `Ticket.CustomerId` pairs with implicit ratings based on resolution time and customer satisfaction.

#### Configuration Example (`masala_config.json`)

```json
"Dispatching": {
  "IsEnabled": true,
  "MinHistoryForAffinityMatch": 3,               // Min past interactions for affinity
  "MaxAssignedTicketsPerAgent": 15,
  "RetrainRecommendationModelFrequencyHours": 24
}
```

---

### 5. A - Anticipation (The Weather Report)

-   **Business Problem:** Reactive capacity planning; understaffing is only realized when backlogs are overflowing.
-   **Current Implementation:** **Interface defined, not yet implemented.**
-   **Tech Stack:** ML.NET **Time Series Forecasting (SSA - Singular Spectrum Analysis)**.
-   **Logic:** 
    - Analyzes historical daily ticket inflow over past 3 years (`InflowHistoryYears`)
    - Detects seasonal patterns (e.g., tax filing deadlines, holiday periods)
    - Forecasts ticket volume for next 30 days (`ForecastHorizonDays`)
    - Compares predicted inflow vs. team capacity (agent count × velocity - planned absences)
-   **Action:** Triggers "Director Alerts" if forecasted inflow exceeds capacity within a 30-day horizon, enabling proactive resource management (hiring temps, overtime approval, vacation freezes).
-   **ML Model:** SSA time series model trained on `COUNT(Tickets) GROUP BY CreationDate`.

#### Configuration Example (`masala_config.json`)

```json
"Anticipation": {
  "IsEnabled": true,
  "ForecastHorizonDays": 30,
  "InflowHistoryYears": 3,
  "CapacityRefreshFrequencyHours": 12,
  "RiskThresholdPercentage": 20                  // Alert if inflow > capacity by 20%
}
```

## The Fuel: Synthetic Data Generation

Due to privacy and sensitivity of real operational data, the project relies on robust mock data generation:

-   **Tool:** The **Bogus** library for .NET
-   **Strategy:** Creates **synthetic data** that preserves statistical properties of real-world environments
-   **Intentional Patterns for GERDA:**
    -   **Spam Patterns:** A few clients generating high volumes of tickets in short time windows
    -   **Complexity Patterns:** Certain categories consistently having longer resolution times
    -   **Seasonality Patterns:** Ticket volumes spiking in specific, predictable months (e.g., tax deadlines)
-   **Benefit:** Allows development, testing, and demonstration of complete Ticket Masala and GERDA feature set in a safe, privacy-preserving environment

## Current Implementation Status (Sprint 5 - December 2024)

| Component | Status | Technology | Notes |
|-----------|--------|------------|-------|
| **G - Grouping** | ✅ **Implemented** | Rule-based LINQ | Detects spam by counting tickets per customer in time window |
| **E - Estimating** | ✅ **Implemented** | Config lookup table | Maps categories to Fibonacci points via JSON |
| **R - Ranking** | ⏳ **Interface Only** | WSJF Algorithm | Planned for Sprint 6 |
| **D - Dispatching** | ⏳ **Interface Only** | ML.NET Matrix Factorization | Planned for Sprint 6-7 |
| **A - Anticipation** | ⏳ **Interface Only** | ML.NET Time Series SSA | Planned for Sprint 7 |

### Next Steps

**Sprint 6 (Dec 8-14):**
-   Implement R - Ranking service with WSJF algorithm
-   Implement D - Dispatching service with ML.NET Matrix Factorization
-   Create database migration for GERDA fields
-   Integrate GERDA services into TicketController

**Sprint 7 (Dec 15-21):**
-   Implement A - Anticipation service with ML.NET Time Series forecasting
-   Add ML.NET NuGet packages
-   Create UI components to display GERDA insights (priority scores, agent recommendations, capacity alerts)
-   Performance testing and optimization
