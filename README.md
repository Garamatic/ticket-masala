# IT-Project-25/26 - Ticket Masala

![Logo](src/TicketMasala.Web/wwwroot/images/full-logo.png)

## 📌 Info

- **Team**: Charlotte Schröer, Maarten Görtz, Wito De Schrijver, Juan Benjumea
- **Branch**: `main`
- **Concept**: Ticketing- en Projectmanagement met AI-ondersteuning (GERDA)
- **Technologieën**: .NET 10, ASP.NET Core MVC, Entity Framework Core, SQLite

---

## 🧠 Projectoverzicht

Ticket Masala is een platform voor het centraal beheren van support-tickets en projecttaken binnen een organisatie. De belangrijkste functies zijn:

- **Ticketbeheer**: Aanmaken, volgen en afhandelen van support-tickets via een intuïtieve interface.
- **Projectmanagement**: Taken, deadlines en resources worden efficiënt beheerd binnen projectteams.
- **AI-functionaliteit (GERDA)**: Automatische ticketclassificatie en toewijzing aan de juiste medewerker op basis van domein, werkdruk en expertise. De AI ondersteunt ook tijdsinschattingen en prioritering.
- **Notificatiesysteem**: Gebruikers ontvangen relevante updates over tickets en projecten.
- **Importmogelijkheden**: Informatie kan worden geïmporteerd via CSV en e-mail.
- **Beheer-paneel**: Voor configuratie, gebruikersbeheer en het aanpassen van domeinen/strategieën.

Het systeem is modulair opgezet en eenvoudig uit te breiden met nieuwe functionaliteit.

---

## 🎯 Waarom Ticket Masala?

Ticket Masala brengt ticketbeheer en projectmanagement samen op één platform voor moderne organisaties. Door AI-ondersteunde ticketdistributie worden supportvragen sneller en eerlijker verdeeld over medewerkers, wat de efficiëntie en klanttevredenheid verhoogt. Het management krijgt realtime inzicht in voortgang en knelpunten, en het systeem is schaalbaar en aanpasbaar voor diverse bedrijfsprocessen.

---

## 📋 Prerequisites

- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com/download)
- **Docker** (optional) - For containerized deployment

> **Note**: No additional dependencies required. SQLite database is created automatically on first run.

---

## 🚀 Snelstart

### Option 1: Local Development

```bash
# Clone the repository
git clone https://github.com/your-org/ticket-masala.git
cd ticket-masala

# Build
dotnet build

# Start (creates and seeds database on first run)
dotnet run --project src/TicketMasala.Web/

# Run tests
dotnet test
```

De app draait standaard op `http://localhost:5054`.

### Option 2: Docker

```bash
# Build and run with Docker Compose
docker-compose up --build

# Or build and run manually
docker build -t ticket-masala .
docker run -p 5054:8080 ticket-masala
```

De app draait op `http://localhost:5054`.

---

## 🔑 Testaccounts

De database wordt bij eerste gebruik automatisch gevuld ("ge-seed").

| Rol | E-mail | Wachtwoord |
|------|-------|----------|
| **Admins** | | `Admin123!` |
| Admin | `admin@ticketmasala.com` | `Admin123!` |
| CEO | `sarah.admin@ticketmasala.com` | `Admin123!` |
| **Werknemers** | | `Employee123!` |
| Projectmanager | `mike.pm@ticketmasala.com` | `Employee123!` |
| Projectmanager | `lisa.pm@ticketmasala.com` | `Employee123!` |
| Support | `david.support@ticketmasala.com` | `Employee123!` |
| Support (EU) | `claude.support@ticketmasala.com` | `Employee123!` |
| Support (Benelux) | `pieter.support@ticketmasala.com` | `Employee123!` |
| Support | `emma.support@ticketmasala.com` | `Employee123!` |
| Finance | `robert.finance@ticketmasala.com` | `Employee123!` |
| **Klanten** | | `Customer123!` |
| Klant | `alice.customer@example.com` | `Customer123!` |
| Klant | `bob.jones@example.com` | `Customer123!` |
| Klant | `carol.white@techcorp.com` | `Customer123!` |
| Klant | `daniel.brown@startup.io` | `Customer123!` |
| Klant | `emily.davis@enterprise.net` | `Customer123!` |

> Seed-data wordt gedefinieerd in `config/seed_data.json`.

---

## 🏗️ Projectstructuur

```
ticket-masala/
├── src/
│   ├── TicketMasala.Web/          # Hoofd ASP.NET Core MVC-applicatie
│   │   ├── Controllers/           # MVC controllers + API
│   │   ├── Engine/                # Business logic en services
│   │   │   ├── Core/              # Tickets, projecten, notificaties
│   │   │   ├── GERDA/             # AI-dispatch & inschattingen
│   │   │   ├── Compiler/          # Regelsysteem
│   │   │   └── Ingestion/         # CSV/E-mail import
│   │   ├── Data/                  # EF Core DbContext, Seeder
│   │   ├── Models/                # Domein-entity's
│   │   └── Views/                 # Razor views (frontend)
│   └── TicketMasala.Tests/        # Unit- en integratietests
├── config/                        # Appconfiguraties & data
│   ├── masala_config.json         # Feature-flags
│   ├── masala_domains.yaml        # GERDA domeinstrategieën
│   └── seed_data.json             # Database seed-data
├── deploy/                        # Deployment scripts & documentatie
├── Dockerfile                     # Docker build
├── docker-compose.yml             # Docker Compose
└── fly.toml                       # Fly.io configuratie
```

---

## 🛠️ Problemen oplossen

**Databaseproblemen?** Verwijder en herstart:
```bash
rm -f src/TicketMasala.Web/app.db*
dotnet run --project src/TicketMasala.Web/
```

**Poort in gebruik?** Standaard is `5054`. Aanpassen in `Properties/launchSettings.json`.

**Tests:** 142 tests, alle geslaagd.

---

## 📚 Documentatie

- **API documentatie**: Swagger UI beschikbaar op `/swagger` bij draaiende app
- **Deployment gidsen**: `deploy/` directory
- **Demo script**: `docs/demo/demo_script.md` - Gebruiksscenario's voor demonstraties
- **GERDA domein configuratie**: `config/masala_domains.yaml`
