# Ticket Masala Documentation

Welcome to the Ticket Masala documentation! This document provides a comprehensive overview of the project, its architecture, and how to get started.

---

## 1. Overview

Ticket Masala is a modular monolith application designed to streamline IT ticketing workflows with AI-powered automation. The project leverages modern .NET technologies and a configuration-driven architecture to ensure scalability and adaptability across various domains.

### Key Features

- **AI Augmentation**: GERDA AI pipeline for ticket grouping, ranking, and dispatching.
- **Configuration-Driven**: YAML-based rules for extensibility.
- **Frontend Design System**: Consistent and professional UI components.
- **Modular Architecture**: Clear separation of concerns with reusable patterns.

### Project Goals

1. Simplify IT ticketing workflows.
2. Enhance decision-making with AI.
3. Provide a scalable and extensible platform.

---

## 2. Table of Contents

1. **Overview**: High-level introduction to the project.
2. **Architecture Summary**: Detailed explanation of the system's modular monolith design.
3. **AI Strategy**: Insights into the GERDA AI pipeline.
4. **Frontend Design System**: Guidelines for building consistent user interfaces.
5. **Configuration & Extensibility**: Blueprint for transforming the system into a configuration-driven engine.

---

## 3. Architecture Summary

### Architecture at a Glance

**Type:** Modular Monolith with AI Augmentation  
**Stack:** ASP.NET Core MVC + EF Core + ML.NET

```text
Presentation → Services → Repositories → Database
                  ↓
              Observers → GERDA AI
```

### Key Design Patterns

| Pattern | Purpose | Location |
|---------|---------|----------|
| **Observer** | Event-driven notifications | `Observers/` |
| **Repository + UoW** | Data access abstraction | `Repositories/` |
| **Specification** | Reusable queries | `Repositories/Specifications/` |
| **Strategy** | Swappable AI algorithms | `Services/GERDA/` |
| **Facade** | AI subsystem orchestration | `GerdaService` |
| **Factory** | Object creation | `TicketFactory` |

### GERDA AI Modules

| Letter | Module | Technique |
|--------|--------|-----------|
| **G** | Grouping | K-Means (spam detection) |
| **E** | Estimating | Classification (effort) |
| **R** | Ranking | WSJF (priority) |
| **D** | Dispatching | Matrix Factorization (agent matching) |
| **A** | Anticipation | Time Series (forecast) |

### Service Architecture (CQRS-lite)

| Interface | Responsibility |
|-----------|---------------|
| `ITicketQueryService` | Read operations |
| `ITicketCommandService` | Write operations |
| `ITicketFactory` | Ticket creation |

### Key Decisions

| Decision | Rationale |
|----------|-----------|
| Local ML.NET | GDPR privacy, no API costs |
| Modular Monolith | Simpler ops than microservices |
| Observer Pattern | AI processing doesn't slow UI |
| Repository Pattern | Testable, database-agnostic |

---

## 4. Quick Start for Developers

1. **`Program.cs`** → DI setup
2. **`DbSeeder.cs`** → Sample data
3. **`TicketService.cs`** → Business logic
4. **`GerdaService.cs`** → AI hub

---

For detailed instructions on deployment, see the `how-to-deploy` folder in the root directory.