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

### API
- **[API Reference](api/API_REFERENCE.md)**: REST API endpoints, authentication, request/response formats

### Architecture
- **[Summary](architecture/SUMMARY.md)**: Architecture at a glance
- **[Detailed Architecture](architecture/DETAILED.md)**: Configuration & extensibility design
- **[Controllers](architecture/CONTROLLERS.md)**: MVC and API controller patterns
- **[Domain Model](architecture/DOMAIN_MODEL.md)**: Core entities and relationships
- **[Repositories](architecture/REPOSITORIES.md)**: Data access patterns (Repository, UoW, Specification)
- **[Observers](architecture/OBSERVERS.md)**: Event-driven Observer pattern
- **[Middleware](architecture/MIDDLEWARE.md)**: Custom middleware components
- **[Extensions](architecture/EXTENSIONS.md)**: DI registration extension methods
- **[GERDA AI Modules](architecture/gerda-ai/GERDA_MODULES.md)**: G.E.R.D.A. AI pipeline documentation

### Guides
- **[Development](guides/DEVELOPMENT.md)**: Local development setup and workflow
- **[Testing](guides/TESTING.md)**: Test project structure and patterns
- **[Configuration](guides/CONFIGURATION.md)**: YAML/JSON configuration guide
- **[Troubleshooting](guides/TROUBLESHOOTING.md)**: Common issues and solutions
- **[Data Seeding](guides/DATA_SEEDING.md)**: Database seed data configuration

### Deployment
- **[Fly.io](deployment/FLY_IO.md)**: Fly.io deployment guide
- **[Docker](deployment/DOCKER_GUIDE.md)**: Docker containerization
- **[CI/CD](deployment/CI_CD.md)**: GitHub Actions pipeline

### Project Management
- **[Roadmap](project-management/roadmap_v3.md)**: Implementation roadmap and phases

### Assets
- **[Screenshots & Visuals](assets/)**: UI screenshots and presentation materials

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