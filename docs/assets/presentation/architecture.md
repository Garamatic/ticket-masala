# Ticket Masala - Architectuur Overzicht

## Systeemarchitectuur

```
┌─────────────────────────────────────────────────────────────┐
│                    Ticket Masala Platform                   │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │   Frontend   │  │  API Layer   │  │  Business Logic  │   │
│  │  Razor Views │◄─│  Controllers │◄─│  Engine/Services │   │
│  │  Bootstrap 5 │  │  REST + MVC  │  │  GERDA AI        │   │
│  └──────────────┘  └──────────────┘  └──────────────────┘   │
│                                              │               │
│                                     ┌────────▼────────┐     │
│                                     │   Data Layer    │     │
│                                     │  EF Core + SQLite│     │
│                                     └─────────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

## Kerncomponenten

| Component | Beschrijving |
|-----------|-------------|
| **Engine/Core** | Ticket- en projectbeheer services |
| **Engine/GERDA** | AI-gestuurde dispatching en forecasting |
| **Engine/Ingestion** | CSV en e-mail import |
| **Data** | Entity Framework DbContext, seeders |
| **Views** | Razor views met Bootstrap 5 |

## Technologiestack

- **Backend**: .NET 10, ASP.NET Core MVC
- **Database**: Entity Framework Core + SQLite
- **AI/ML**: ML.NET voor time series forecasting
- **Frontend**: Razor Views, Bootstrap 5, Chart.js

## Deployment Opties

1. **Docker** - `docker-compose up`
2. **Fly.io** - Zie `deploy/README-deploy-fly.md`
3. **Lokaal** - `dotnet run`
