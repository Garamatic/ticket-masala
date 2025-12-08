# Frontend setup — v3 (aligned to current project)

This document reflects the latest state of the `TicketMasala` codebase, incorporating architectural updates and best practices for the frontend.

## High-level overview

- **App type:** Server-rendered ASP.NET Core MVC app using Razor Views, TagHelpers, and HTMX for interactivity (`src/TicketMasala.Web`).
- **Static assets:** Served from `wwwroot` (JS/CSS/images). The production UI is Razor-based, with minimal reliance on JavaScript frameworks.
- **Frontend responsibilities:** Render pages, provide declarative interactivity (HTMX), and integrate with backend services via Razor controllers returning HTML partials.

## Client views (actual)

- **Project overview (client):** Implemented as a Razor view under `Views/Projects` — dynamic, grid-like rendering from view models. Includes thumbnails in `wwwroot/project-thumbnails/`.
- **Project detail (client):** Razor pages with ticket list, milestones, and basic chat UI (server-backed). Chat is implemented as a controller-backed feature, not a separate real-time SPA.
- **Create project / Add project:** Form-based via controller actions that create tickets and seed project entities.

## Project manager (actual)

- **Project dashboard (PM):** Table or card views implemented with Razor and server-side pagination/filtering. ViewModels live in `ViewModels/`.
- **Project creation (PM):** Form with optional AI context is invoked server-side; backend generates tickets and milestones.
- **Ticketing / assignment:** Managed via controllers; assignment and re-assignment is server-side functionality.

## Staff view (actual)

- **Ticketing (staff):** Razor views show assigned tickets; editing is server-side POST actions.
- **Project view (staff):** Read/write based on authorization roles.

## GERDA Dispatch View

### Overview

The GERDA Dispatch view is an AI-powered ticket assignment dashboard designed for project managers. It provides insights into low-confidence allocations, agent availability, and dispatch statistics, enabling efficient ticket allocation.

### Features

- **Low Confidence Allocations:** Displays a list of tickets requiring manual review due to low AI confidence.
- **Agent Availability:** Shows a list of available agents and their current workload.
- **Dispatch Statistics:** Provides metrics related to ticket dispatching, such as average assignment time and backlog size.
- **Project Options:** Lists available projects for ticket reassignment.

### ViewModel: `GerdaDispatchViewModel`

The `GerdaDispatchViewModel` is the data structure backing the GERDA Dispatch view. It includes the following properties:

- **`LowConfidenceAllocations`**: A list of `TicketDispatchInfo` objects representing tickets requiring manual review. Each ticket includes:
  - `Guid`: Unique identifier.
  - `Description`: Brief description of the ticket.
  - `EstimatedEffortPoints`: AI-estimated effort required.
  - `PriorityScore`: AI-calculated priority score.
  - `RecommendedProjectName`: Suggested project for assignment.
  - `CurrentProjectName`: Current project (if any).

- **`AvailableAgents`**: A list of `AgentInfo` objects detailing agents available for ticket assignment.
- **`Statistics`**: A `DispatchStatistics` object containing metrics like backlog size and average assignment time.
- **`Projects`**: A list of `ProjectOption` objects representing available projects for ticket reassignment.

### Razor View: `DispatchBacklog.cshtml`

- **Location:** `Views/Manager/DispatchBacklog.cshtml`
- **Title:** "GERDA Dispatch Backlog"
- **Description:** "AI-powered ticket assignment dashboard"
- **Styling:** Includes custom CSS for headers, cards, and ticket details.

### Alignment with Frontend Setup

The GERDA Dispatch view aligns with the "Project manager view" section in the frontend setup. It enhances the ticketing and assignment functionality by leveraging AI recommendations and providing actionable insights for project managers.

## Updated Frontend Tooling

- **Interactivity:** HTMX is used for declarative interactivity (e.g., `hx-post`, `hx-target`).
- **Dependencies:** No Node.js or `package.json`. Libraries like HTMX are served directly from `wwwroot/lib` or CDNs.
- **State Management:** Server-side state with Razor HTML.
- **CSS:** Standard CSS without a build step.

### HTMX Integration

HTMX has been added to `_Layout.cshtml` to enable declarative interactivity. JSON APIs are replaced with Razor controllers returning HTML partials.

## Dev workflow / dev commands

- Restore/build/tests:

  ```bash
  dotnet restore
  dotnet build
  dotnet test
  ```

- Run the web app (watch mode — recommended during frontend development):

  ```bash
  dotnet watch --project src/TicketMasala.Web run
  ```

- To run just the web project without watch:

  ```bash
  dotnet run --project src/TicketMasala.Web
  ```

- If using Docker (Dockerfile exists in `src/TicketMasala.Web`):

  ```bash
  docker build -t ticket-masala-web -f src/TicketMasala.Web/Dockerfile .
  docker run --rm -p 5000:80 ticket-masala-web
  ```

## Data & migrations

- DB migrations are located in `Migrations/`. To apply migrations / update DB (dev):

  ```bash
  dotnet ef database update --project src/TicketMasala.Web
  ```

## Repository hygiene / gotchas

- **Accidental DB files:** Ensure `*.db`, `*.db-wal`, and `*.db-shm` are ignored in `.gitignore`.
- **Static assets:** Keep `wwwroot/project-thumbnails/` committed, but regenerate images/build artifacts locally rather than committing large build outputs.
- **Frontend tooling:** Avoid Node.js. Use HTMX and LibMan for frontend dependencies.

## Accessibility, i18n & localization

- The site uses localized strings (tests reference localized content). Keep localized resources in `Resources/` and ensure the `RequestLocalization` pipeline remains configured in `Program.cs`.
- When editing views, verify localized text via integration tests in `IT-Project2526.Tests`.

## Testing notes

- Integration tests run the app in-memory (WebApplicationFactory). If tests fail during startup, ensure services registered in `Program.cs` (e.g., GERDA strategies) have working defaults or test-friendly fallbacks.

---

This document reflects the latest architectural decisions and ensures alignment with the "Masala Lite" monolith approach.