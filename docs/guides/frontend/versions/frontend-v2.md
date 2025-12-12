# Frontend setup — v2 (aligned to current project)

This document replaces/updates the original `Frontend setup` notes to match the actual state of the `TicketMasala` codebase (ASP.NET Core Razor web app, not a pure SPA).

## High-level overview

- **App type:** Server-rendered ASP.NET Core MVC app using Razor Views and TagHelpers (`src/TicketMasala.Web`).
- **Static assets:** Served from `wwwroot` (JS/CSS/images). Some sample frontend code exists in `samples/` but the production UI is Razor-based.
- **Frontend responsibilities:** Render pages, provide client-side interactivity (vanilla JS/optional small libs), and integrate with backend services (controllers + APIs in `Controllers/Api`).

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

The GERDA Dispatch view is an AI-powered ticket assignment dashboard designed for project managers. It provides insights into unassigned tickets, agent availability, and dispatch statistics, enabling efficient ticket allocation.

### Features

- **Unassigned Tickets:** Displays a list of tickets with AI-generated recommendations for assignment.

- **Agent Availability:** Shows a list of available agents and their current workload.

- **Dispatch Statistics:** Provides metrics related to ticket dispatching, such as average assignment time and backlog size.

- **Project Options:** Lists available projects for ticket reassignment.

### ViewModel: `GerdaDispatchViewModel`

The `GerdaDispatchViewModel` is the data structure backing the GERDA Dispatch view. It includes the following properties:

- **`UnassignedTickets`**: A list of `TicketDispatchInfo` objects representing tickets awaiting assignment. Each ticket includes:
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

## Important frontend-related folders/files

- `Views/` — all Razor views for pages and partials.
- `ViewModels/` — models passed from controllers to views.
- `TagHelpers/` — custom Razor tag helpers used across views.
- `wwwroot/` — JS/CSS/images (client assets). Put bundling/build outputs here.
- `src/TicketMasala.Web/Controllers/Api` — endpoints consumed by client-side code (AJAX/fetch calls).
- `src/TicketMasala.Web/docs/frontend-architecture/` — docs (this file).

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

- There is a runtime DB file referenced in the tree — the SQLite WAL `app.db-wal` was committed accidentally. See "Repository hygiene" below.

## Repository hygiene / gotchas

- **Accidental DB files:** `src/TicketMasala.Web/app.db-wal` is a runtime SQLite file and should not be committed. Add `*.db`, `*.db-wal`, and `*.db-shm` to `.gitignore`.
- **Static assets:** Keep `wwwroot/project-thumbnails/` committed, but regenerate images/build artifacts locally rather than committing large build outputs.
- **Frontend tooling:** The repo does not include a heavy frontend build pipeline (webpack/rollup). If you introduce a modern SPA or bundler, add a `package.json` and `.gitignore` entries for `node_modules/`.

## Accessibility, i18n & localization

- The site uses localized strings (tests reference localized content). Keep localized resources in `Resources/` and ensure the `RequestLocalization` pipeline remains configured in `Program.cs`.
- When editing views, verify localized text via integration tests in `IT-Project2526.Tests`.

## Testing notes

- Integration tests run the app in-memory (WebApplicationFactory). If tests fail during startup, ensure services registered in `Program.cs` (e.g., GERDA strategies) have working defaults or test-friendly fallbacks.

## Recommendations / Next steps (practical)

- **Immediate:** Add `.gitignore` entries for SQLite files and remove `src/TicketMasala.Web/app.db-wal` from the repo history (or at least remove it and commit the deletion):

  ```bash
  # add to .gitignore
  echo "*.db" >> .gitignore
  echo "*.db-wal" >> .gitignore
  # remove file and commit
  git rm --cached src/TicketMasala.Web/app.db-wal
  git commit -m "chore: remove accidental SQLite WAL and ignore DB files"
  git push
  ```

  - **Updated Frontend Tooling**

- **Interactivity:** HTMX is used for declarative interactivity (e.g., `hx-post`, `hx-target`).
- **Dependencies:** No Node.js or `package.json`. Libraries like HTMX are served directly from `wwwroot/lib` or CDNs.
- **State Management:** Server-side state with Razor HTML.
- **CSS:** Standard CSS without a build step.

### HTMX Integration

HTMX has been added to `_Layout.cshtml` to enable declarative interactivity. JSON APIs are replaced with Razor controllers returning HTML partials.

- **Local dev:** Use `dotnet watch` while iterating on Razor views. Use browser dev tools to test AJAX endpoints in `Controllers/Api`.
- **CI / Tests:** Fix startup registration failures (e.g., missing GERDA strategy registrations) so `dotnet test` works in CI.

---

If you want, I can:

- Create/update a `.gitignore` and remove the committed WAL file and push the change.
- Add this v2 file into the docs folder and commit/push it (I can do that now).
- Scaffold a minimal `package.json` and frontend build scripts if you plan to use bundlers.

Which of these should I do next? (I can create the `.gitignore` change + remove WAL and commit it, plus add this file.)
