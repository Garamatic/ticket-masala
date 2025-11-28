# Project Name: G.E.R.D.A. (GovTech Extended Resource Dispatch & Anticipation)

## Mission
Move from "Passive Ticket Tracking" to "Active Operational Orchestration" within the "Ticket Masala" platform.

## High-Level Architecture: "Ticket Masala" (White Label Ticketing Engine)

"Ticket Masala" is designed as a generic, configurable ticketing system that can adapt to various organizational needs (e.g., IT Helpdesk, Tax Office). Its core principles are:

-   **Modular Architecture:** Clear separation of concerns between the core ticketing logic and the intelligent "GERDA" services.
-   **Configuration over Code:** Behaviors, roles, and categories are defined via a central JSON configuration file, allowing for flexible deployment without code changes.
-   **Role-Based Access Control (RBAC):** Supports a flexible hierarchy of users (Superuser, Project Manager, Handler, Viewer).
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
-   **Tech Stack:** ML.NET **Clustering (K-Means)** or rule-based grouping (configured via JSON).
-   **Logic:** Detects clusters of incoming requests based on `RequesterId` and `TimeWindow`.
-   **Action:** Auto-bundles multiple related tickets into one "Parent Case" to streamline processing and reduce agent workload.

#### Configuration Example (`masala_config.json`):
```json
"SpamDetection": {
  "TimeWindowMinutes": 60,
  "MaxTicketsPerUser": 5,  // If > 5 tickets from same user in 1 hour -> CLUSTER THEM
  "Action": "AutoMerge"    // or "Flag" for manual review
}
```

---

### 2. E - Estimating (The Sizer)

-   **Business Problem:** Treating all tasks equally, regardless of actual effort, leads to inefficient queue management.
-   **Tech Stack:** ML.NET **Multi-Class Classification**.
-   **Logic:** Predicts complexity buckets (**S / M / L / XL**) for tickets based on their `Category` and historical cycle times of similar cases. This can use a pre-trained ML model or a simple lookup based on `ComplexityMap` from config.
-   **Action:** Assigns a "Fibonacci Point Value" (1, 3, 8, 13, etc.) to the ticket for ranking.

#### Configuration Example (`masala_config.json`):
```json
"ComplexityMap": {
  "Password Reset": 1,      // S (e.g., Tax equivalent: Address Change)
  "Hardware Request": 3,    // M
  "Software Bug": 8,        // L
  "System Outage": 13       // XL (e.g., Tax equivalent: Audit)
}
```

---

### 3. R - Ranking (The Prioritizer)

-   **Business Problem:** Agents often cherry-pick easy tasks, causing urgent/hard tasks to breach SLAs.
-   **Tech Stack:** **WSJF Algorithm** (Weighted Shortest Job First).
-   **Logic:** Calculates a `PriorityScore` dynamically for each ticket using the formula:
    $$Priority = \frac{\text{Cost of Delay (SLA Breach Risk)}} {\text{Job Size (Fibonacci Points)}}$$ 
    -   `Cost of Delay`: Derived from `SlaConfig` and the ticket's age.
    -   `Job Size`: Obtained from the Estimating (E) step.
-   **Action:** Re-orders the queue dynamically to prioritize high-value, low-effort tasks and prevent SLA breaches.

#### Configuration Example (`masala_config.json`):
```json
"SlaConfig": {
  "DefaultDays": 7,       // Default SLA for a ticket
  "CriticalDays": 1       // Days remaining until SLA breach becomes critical
}
```

---

### 4. D - Dispatching (The Matchmaker)

-   **Business Problem:** Loss of institutional knowledge when new agents handle clients with complex history; inefficient assignment to overloaded agents.
-   **Tech Stack:** ML.NET **Matrix Factorization (Recommendation)**.
-   **Logic:** Scores `[Agent, Client]` pairs based on historical case handling. Also considers agent availability and current workload.
-   **Action:** Recommends the most suitable agent for the highest-priority cases, balancing affinity with workload.

#### Configuration Example (`masala_config.json`):
```json
"Recommender": {
  "RetrainFrequencyHours": 24,
  "MinHistoryForMatch": 3   // Minimum number of past interactions to consider an affinity match
}
```

---

### 5. A - Anticipation (The Weather Report)

-   **Business Problem:** Reactive capacity planning; understaffing is only realized when backlogs are overflowing.
-   **Tech Stack:** ML.NET **Time Series Forecasting (SSA)** combined with HR/Agent availability data.
-   **Logic:** Compares **Predicted Inflow** (seasonal trends in new tickets) vs. **Predicted Capacity** (agent velocity, holidays, sick days).
-   **Action:** Triggers "Director Alerts" if forecasted inflow exceeds capacity within a 30-day horizon, enabling proactive resource management.

#### Configuration Example (`masala_config.json`):
```json
"Anticipation": {
  "ForecastHorizonDays": 30,
  "InflowHistoryYears": 3,
  "CapacityRefreshFrequencyHours": 12
}
```

## Impact & Value Proposition

-   **For University:** Demonstrates advanced concepts in AI/ML (.NET, multiple ML algorithms), modular software architecture, and flexible design patterns (Strategy Pattern, Configuration over Convention).
-   **For Business:** Transforms reactive operations into a proactive, data-driven system, leading to:
    -   Reduced SLA breaches.
    -   Improved agent efficiency and workload balance.
    -   Better resource planning and early bottleneck detection.
    -   Enhanced institutional knowledge retention.
