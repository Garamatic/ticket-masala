# Repository Structure Documentation

## Overview

This document provides an updated overview of the repository structure for the `ticket-masala` project. The structure reflects the latest changes and aligns with the v3.0 architecture specification, ensuring modularity, scalability, and maintainability.

---

## Top-Level Structure

```plaintext
/
├── build_log.txt
├── build_output.txt
├── docker-compose.yml
├── README.md
├── TicketMasala.sln
├── config/
├── dev-scripts/
├── how-to-deploy/
├── samples/
├── src/
│   ├── TicketMasala.Tests/
│   └── TicketMasala.Web/
│       ├── appsettings.*.json
│       ├── Dockerfile
│       ├── Engine/
│       │   ├── Compiler/
│       │   ├── GERDA/
│       │   │   ├── Configuration/
│       │   │   ├── Tickets/
│       │   └── Ingestion/
│       │       ├── Background/
│       │       ├── Validation/
│       ├── Controllers/
│       ├── Data/
│       ├── docs/
│       ├── Models/
│       ├── Repositories/
│       ├── Utilities/
│       ├── Views/
│       └── wwwroot/
```

---

## Key Directories

### `src/TicketMasala.Web/Engine/`

The `Engine/` directory contains the core processing engines for the application. It is divided into the following subfolders:

- **`Compiler/`**: Handles compilation-related tasks, including the `RuleCompilerService` for converting YAML rules into compiled .NET delegates.
- **`GERDA/`**: Contains GERDA-specific logic, including:
  - `Configuration/`: Domain configuration services.
  - `Tickets/`: Ticket-related services and logic.
- **`Ingestion/`**: Manages data ingestion, including:
  - `Background/`: Background processing services.
  - `Validation/`: Validation services.

### `src/TicketMasala.Web/Controllers/`

Contains the controllers responsible for handling HTTP requests and routing them to the appropriate services.

### `src/TicketMasala.Web/Data/`

Contains the `MasalaDbContext` and related data access logic. SQLite is configured with `PRAGMA journal_mode=WAL` for improved concurrency. High-traffic fields are mapped to generated columns for performance optimization.

### `src/TicketMasala.Web/docs/`

Documentation related to the project, including architecture, deployment, and development guides.

### `src/TicketMasala.Web/wwwroot/`

Static files such as JavaScript, CSS, and images for the frontend.

---

## Notes

- The repository structure follows a modular monolith architecture.
- Legacy files and directories have been cleaned up to align with the v3.0 specification.
- The `Engine/` directory is the primary location for all core processing logic.
- Future updates should maintain this structure to ensure consistency and scalability.
