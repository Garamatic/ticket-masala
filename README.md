# IT-Project-25/26 - Ticket Masala

![Logo](IT-Project2526/docs/visual/logo-green.png)

## 📌 Info (Current State)

- **Team**: Charlotte Schröer, Maarten Görtz, Wito De Schrijver, Juan Benjumea
- **Branch**: `main` (active development branch in this workspace)
- **Concept**: Ticketing, Case, and Project Management with AI support
- **Tech Stack**: .NET 8 (MVC), EF Core, SQLite (local) ---

## 🚀 Quick Start (Updated)

1. Build the solution:

```bash
dotnet build
```

2. Run the Web project (this will apply migrations and attempt to seed the database):

```bash
dotnet run --project src/TicketMasala.Web/
```

Notes:
- The app uses a local SQLite file `app.db` by default. If you have schema issues, you can remove `app.db` and re-run to recreate the database from migrations.
- If seeding fails with an Identity-related error (see Known Issues below), ensure roles are configured or run a one-off DB seed after fixing Identity configuration.

---

## 🔑 Test Accounts (seeded by `DbSeeder` when possible)

**Default Passwords (if seeds run successfully):**

- **Admins**: `Admin123!`
- **Employees**: `Employee123!`
- **Customers**: `Customer123!`

Example seeded users (see `src/TicketMasala.Web/Data/DbSeeder.cs` for details).

---

## 🏗️ Project Structure (high-level)

- `src/TicketMasala.Web/` — ASP.NET Core MVC app, EF Core `MasalaDbContext`, Identity, controllers, views, services.
- `src/TicketMasala.Tests/` — unit/integration tests.
- `src/` — other engines and services (GERDA AI, ingestion, background jobs).

---

## 🛠️ Tech Notes & Known Issues

- Identity and Roles: Recent refactors require `IdentityRole` to be present in the EF model. If seeding fails with "Cannot create a DbSet for 'IdentityRole'" then the app's Identity configuration or `MasalaDbContext` needs to include roles (for example by inheriting from `IdentityDbContext<ApplicationUser, IdentityRole, string>` or registering roles via `AddIdentity<,>().AddRoles<IdentityRole>()`).
- GERDA Strategy Validation: Domain configuration may reference strategies (e.g., `ZoneBased`) that aren't registered yet. If you see "Strategy 'ZoneBased' ... not found" register/implement the strategy or update domain config.
- SQLite / Migrations: SQLite cannot add STORED computed columns via simple ALTER statements; scaffolded migrations were edited to avoid unsupported DDL. If you run into migration errors, consider deleting `app.db` and re-applying migrations after review.
- Port conflicts: Default dev port may be `5054`. If you see "address already in use", free the port or change the listen port in `Properties/launchSettings.json` or `appsettings.Development.json`.

---

## Troubleshooting & Recovery Commands

Delete local SQLite DB and re-apply migrations:

```fish
rm -f app.db
dotnet ef database update --project src/TicketMasala.Web/
dotnet run --project src/TicketMasala.Web/
```

Rebuild and run tests:

```bash
dotnet build
dotnet test
```

---

## Roadmap & Next Steps (developer-facing)

- Compatibility shims are in place to reduce churn after a large model rename — these are temporary and will be removed in a follow-up refactor.

- High-priority tasks:
    - Add or confirm role support in Identity / `MasalaDbContext` (seed blocker).
    - Register missing GERDA strategies referenced by domain configuration.
    - Run and fix failing unit/integration tests.
    - Create an ADR documenting the shim cleanup plan.
