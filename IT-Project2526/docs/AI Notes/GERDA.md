# GERDA: Building an Intelligent Operations Cockpit

This document outlines the architectural journey and design principles for the "BF Masala" project, which evolved into a generic, intelligent ticketing system named "Ticket Masala" powered by the "GERDA" AI engine.

## 1. The Core Problem: The Legacy System Bottleneck

Many large organizations (governments, banks, insurance companies) rely on a robust but cumbersome "System of Record" (e.g., SAP). While this system is the legal source of truth, it is often slow, difficult to manage, and lacks the agility required for modern operational management.

-   **The System of Record:** Legally required, holds master data, but is inefficient for day-to-day work allocation and tracking.

-   **The Need:** A fast, modern "cockpit" for team leaders and directors to distribute work, track team velocity, monitor SLAs, and identify operational bottlenecks in real-time.

The goal of this project is to build an "Operational Overlay" that sits on top of the legacy system, providing the necessary agility without replacing the core infrastructure.

## 2. The Solution: A "Digital Twin" Operations Cockpit

The proposed solution is a .NET application that acts as a "Digital Twin" of the operational backlog. It provides a modern interface for work management while the legacy system remains the "database of truth."

-   **.NET App (Ticket Masala):** Where work is actively _managed_, prioritized, and dispatched for operational efficiency.
-   **Legacy System (e.g., SAP):** Where the case officially _lives_ and is legally recorded.

This architecture is "Read-Heavy, Write-Light." The primary data flow is a periodic one-way sync from the legacy system to the .NET app's database. This populates the management cockpit with real data without overwhelming the legacy system with real-time queries.

### Data Ingestion Strategies

1.  **The "Senior Architect" Way (API Integration):** The .NET app connects directly to the legacy system's APIs (e.g., SAP .NET Connector using BAPIs) to pull data programmatically. This is the most robust and professional method.
2.  **The "Hacker" Way (File Drop):** A manager exports the open case list from the legacy system to a spreadsheet (`.xlsx`). The .NET application monitors a shared folder and uses a file watcher to parse this spreadsheet and update its internal database. This method requires zero permissions from IT and can be implemented quickly to prove value.

## 3. The Core Architecture: Ticket Masala

To meet both university and real-world requirements, the project is designed as a generic, configurable "white-label" ticketing engine called **Ticket Masala**. This approach separates the core application from any specific business logic.

-   **Technology:** A standard .NET MVC/Core application.
-   **Data Layer:** Entity Framework for database interactions.
-   **Configuration:** A central `masala_config.json` file defines the application's behavior, including roles, work queues (projects), and AI settings. This allows the application to be configured for different domains (e.g., IT Helpdesk, Tax Office) without changing the code.

### The "Project -> Ticket" Hierarchy

A core design decision is to use a `Project -> Ticket` hierarchy. This is crucial for scalability and organization. In an operational context, a "Project" is not a software project but a **container for work**.

This "Project" layer can be mapped to:

-   **Workstreams / Queues (Recommended):** "VAT Disputes," "Personal Income Tax Errors," etc. This allows for different permissions and workflows per queue.
-   **Tax Years / Campaigns:** "Tax Year 2023 Claims," "Tax Year 2024 Claims." This helps with reporting and archiving.
-   **Regional Directorates:** "Brussels Region," "Flanders Region." This enables multi-tenancy.

## 4. The Intelligence Engine: G.E.R.D.A.

The "secret sauce" of Ticket Masala is **GERDA (GovTech Extended Resource Dispatch & Anticipation)**, an internal AI service that automates and optimizes work management. GERDA's behavior is entirely driven by the `masala_config.json` file.

### G - Grouping (The Noise Filter)

-   **Problem:** Repetitive requests from the same client clog the backlog.
-   **Solution:** Uses **Clustering (K-Means)** or rule-based logic to detect and bundle these requests into a single parent ticket, reducing noise and redundant work.

### E - Estimating (The Sizer)

-   **Problem:** Not all tasks are equal; a 5-minute fix is different from a 5-day investigation.
-   **Solution:** Uses **Multi-Class Classification** to predict a complexity "T-Shirt Size" (S, M, L, XL) for each ticket based on its category. This is trained on historical cycle times, using statistical methods (like the 25th percentile) to estimate "touch time" from noisy data. This size is converted to Fibonacci points (1, 3, 8, 13) for ranking.

### R - Ranking (The Prioritizer)

-   **Problem:** Agents may cherry-pick easy tasks, leaving urgent, difficult tasks to breach their SLAs.
-   **Solution:** Implements the **Weighted Shortest Job First (WSJF)** algorithm.
$$Priority = \frac{\text{Cost of Delay (SLA Breach Risk)}}{\text{Job Size (Fibonacci Points)}}$$
This dynamically re-orders the queue, pushing tasks with the highest return on investment (high urgency, low effort) to the top.

### D - Dispatching (The Matchmaker)

-   **Problem:** Institutional knowledge is lost when new agents handle clients with a long history. Agents can become overloaded.
-   **Solution:** Uses **Matrix Factorization (Recommendation)** to calculate an "affinity score" between agents and clients based on past interactions. It then recommends the best-fit agent who also has available capacity.

### A - Anticipation (The Weather Report)

-   **Problem:** Operational teams are often reactive, only realizing they are understaffed when the backlog is already overflowing.
-   **Solution:** Uses **Time Series Forecasting (SSA)** to predict future incoming ticket volume based on historical seasonality. This is compared against the team's predicted capacity (factoring in holidays and agent velocity). The system generates "Director Alerts" if a future bottleneck is detected, enabling proactive resource management.

## 5. The Fuel: Synthetic Data Generation

Due to the privacy and sensitivity of real operational data, the project relies on a robust mock data generator.

-   **Tool:** The **Bogus** library for .NET.
-   **Strategy:** The generator creates **synthetic data** that preserves the statistical properties of a real-world environment. It intentionally creates patterns for GERDA to find:
    -   **Spam Patterns:** A few clients generating high volumes of tickets in a short time.
    -   **Complexity Patterns:** Certain categories consistently having longer resolution times.
    -   **Seasonality Patterns:** Ticket volumes spiking in specific, predictable months.

This approach allows for the development, testing, and demonstration of the complete Ticket Masala and GERDA feature set in a safe and private environment.
