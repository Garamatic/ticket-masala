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

## 2. Documentation Structure

This documentation is organized as follows:

- **[Architecture](architecture/SUMMARY.md)**: High-level and detailed architectural designs.
- **[Deployment](deployment/FLY_IO.md)**: Guides for deploying to Fly.io, Docker, and Linux pilots.
- **[Guides](guides/CONFIGURATION.md)**: Configuration, frontend development, and data seeding guides.
- **[Project Management](project-management/roadmap_v3.md)**: Roadmaps, implementation phases, and sprint logs.
- **[Assets](assets/)**: Screenshots, visuals, and presentation materials.

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

### Service Architecture (CQRS-lite)

| Interface | Responsibility |
|-----------|---------------|
| `ITicketQueryService` | Read operations |
| `ITicketCommandService` | Write operations |
| `ITicketFactory` | Ticket creation |

---

## 4. Quick Start for Developers

1. **`Program.cs`** → DI setup
2. **`DbSeeder.cs`** → Sample data
3. **`TicketService.cs`** → Business logic
4. **`GerdaService.cs`** → AI hub

---

For detailed instructions on deployment, see the [deployment](deployment/FLY_IO.md) directory.
For architectural details, see [Detailed Architecture](architecture/DETAILED.md).