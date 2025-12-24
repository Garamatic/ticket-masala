# Blueprint: Development Lifecycle

This guide outlines the development standards, technology stack, and architectural patterns used in the Ticket Masala ecosystem.

---

## The "Monolith First" Doctrine

Ticket Masala is built as a **Modular Monolith**. We prioritize simplicity and local performance over complex microservices.

- **Single Process:** Core services, AI workers, and the web interface run in a single container.
- **SQLite Performance:** We use SQLite for the data layer, leveraging WAL (Write-Ahead Logging) for concurrent access.
- **In-Memory Messaging:** `System.Threading.Channels` replaces external message brokers like RabbitMQ.

---

## Technology Stack

- **Platform:** .NET 10 (C#)
- **Framework:** ASP.NET Core MVC (Web) / Minimal APIs (Gatekeeper)
- **Data Layer:** Entity Framework Core with SQLite
- **Intelligence:** ML.NET (In-Process)
- **Templating:** Scriban (Ingestion Mapping)
- **Frontend:** Vanilla CSS / JS (Professional Design System)

---

## Core Architectural Patterns

### 1. Repository & Unit of Work
Ensures consistent data access and transaction boundaries.

### 2. The Observer Pattern
Decouples core business logic from side-effects (e.g., sending an email or triggering an AI re-ranking).

### 3. Strategy Pattern (GERDA AI)
Allows for swappable algorithms for ranking and dispatching, configured via the DSL.

---

## Getting Started

### 1. Prerequisites
- .NET SDK (latest stable)
- SQLite Tools
- Docker (optional, for localized testing)

### 2. Initial Setup
```bash
git clone ...
cd ticket-masala
dotnet restore
dotnet run --project src/TicketMasala.Web
```

### 3. Working with Seed Data
The system automatically seeds the database from `config/seed_data.json` if it's empty. Use this to quickly set up a test environment with Admin, Employee, and Customer roles.

---

## Quality Standards

- **Compile Safety:** All domain rules must be verified by the Rule Compiler at startup.
- **Performance:** DB queries must use indexed paths (FTS5 / Generated Columns).
- **Security:** CSRF protection and role-based authorization (RBAC) are non-negotiable.

---

## References
- **[Testing Guide](testing.md)**
- **[System Overview](../SYSTEM_OVERVIEW.md)**
