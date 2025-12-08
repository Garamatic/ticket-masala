# IT-Project-25/26 - Ticket Masala

![Logo](src/TicketMasala.Web/wwwroot/images/full-logo.png)

## 📌 Info

- **Team**: Charlotte Schröer, Maarten Görtz, Wito De Schrijver, Juan Benjumea
- **Branch**: `main`
- **Concept**: Ticketing, Case, and Project Management with AI support (GERDA)
- **Tech Stack**: .NET 10, ASP.NET Core MVC, EF Core, SQLite

---

## 🚀 Quick Start

```bash
# Build
dotnet build

# Run (creates database and seeds on first run)
dotnet run --project src/TicketMasala.Web/

# Run tests
dotnet test
```

The app runs at `http://localhost:5054` by default.

---

## 🔑 Test Accounts

The database is seeded automatically on first run.

| Role | Email | Password |
|------|-------|----------|
| **Admins** | | `Admin123!` |
| Admin | `admin@ticketmasala.com` | `Admin123!` |
| CEO | `sarah.admin@ticketmasala.com` | `Admin123!` |
| **Employees** | | `Employee123!` |
| Project Manager | `mike.pm@ticketmasala.com` | `Employee123!` |
| Project Manager | `lisa.pm@ticketmasala.com` | `Employee123!` |
| Support | `david.support@ticketmasala.com` | `Employee123!` |
| Support (EU) | `claude.support@ticketmasala.com` | `Employee123!` |
| Support (Benelux) | `pieter.support@ticketmasala.com` | `Employee123!` |
| Support | `emma.support@ticketmasala.com` | `Employee123!` |
| Finance | `robert.finance@ticketmasala.com` | `Employee123!` |
| **Customers** | | `Customer123!` |
| Customer | `alice.customer@example.com` | `Customer123!` |
| Customer | `bob.jones@example.com` | `Customer123!` |
| Customer | `carol.white@techcorp.com` | `Customer123!` |
| Customer | `daniel.brown@startup.io` | `Customer123!` |
| Customer | `emily.davis@enterprise.net` | `Customer123!` |

> Seed data is defined in `config/seed_data.json`.

---

## 🏗️ Project Structure

```
ticket-masala/
├── src/
│   ├── TicketMasala.Web/          # Main ASP.NET Core MVC app
│   │   ├── Controllers/           # MVC controllers + API
│   │   ├── Engine/                # Business logic services
│   │   │   ├── Core/              # Tickets, Projects, Notifications
│   │   │   ├── GERDA/             # AI dispatch & estimation
│   │   │   ├── Compiler/          # Rule engine
│   │   │   └── Ingestion/         # CSV/Email import
│   │   ├── Data/                  # EF Core DbContext, Seeder
│   │   ├── Models/                # Domain entities
│   │   └── Views/                 # Razor views
│   └── TicketMasala.Tests/        # Unit & integration tests
├── config/                        # App configuration
│   ├── masala_config.json         # Feature flags
│   ├── masala_domains.yaml        # GERDA domain strategies
│   └── seed_data.json             # Database seed data
├── deploy/                        # Deployment scripts & docs
├── Dockerfile                     # Docker build
├── docker-compose.yml             # Docker Compose
└── fly.toml                       # Fly.io config
```

---

## 🛠️ Troubleshooting

**Database issues?** Delete and recreate:
```bash
rm -f src/TicketMasala.Web/app.db*
dotnet run --project src/TicketMasala.Web/
```

**Port conflict?** Default is `5054`. Change in `Properties/launchSettings.json`.

**Tests:** 142 tests, all passing.

---

## 📚 Documentation

- Deployment guides: `deploy/`
- Domain configuration: `config/masala_domains.yaml`
- API: Swagger UI at `/swagger` when running
