# Ticket Masala Documentatie

Welkom bij de Ticket Masala documentatie! Dit document biedt een uitgebreid overzicht van het project, de architectuur en hoe u aan de slag kunt.

---

## 1. Overzicht

Ticket Masala is een modulaire monolithische applicatie ontworpen om IT-ticketingworkflows te stroomlijnen met AI-gestuurde automatisering. Het project maakt gebruik van moderne .NET-technologieën en een configuratiegestuurde architectuur om schaalbaarheid en aanpasbaarheid over verschillende domeinen te garanderen.

### Belangrijkste Kenmerken

- **AI-Augmentatie**: GERDA AI-pipeline voor ticketgroepering, ranking en dispatching.
- **Configuratiegestuurd**: Op YAML gebaseerde regels voor uitbreidbaarheid.
- **Frontend Design System**: Consistente en professionele UI-componenten.
- **Modulaire Architectuur**: Duidelijke scheiding van verantwoordelijkheden met herbruikbare patronen.

### Projectdoelen

1. IT-ticketingworkflows vereenvoudigen.
2. Besluitvorming verbeteren met AI.
3. Een schaalbaar en uitbreidbaar platform bieden.

---

## 2. Documentatiestructuur

Deze documentatie is als volgt georganiseerd:

### API
- **[API Referentie](api/API_REFERENCE.md)**: REST API-endpoints, authenticatie, verzoek/respons-formaten

### Architectuur
- **[Samenvatting](architecture/SUMMARY.md)**: Architectuur in een oogopslag
- **[Gedetailleerde Architectuur](architecture/DETAILED.md)**: Ontwerp voor configuratie en uitbreidbaarheid
- **[Controllers](architecture/CONTROLLERS.md)**: MVC- en API-controllerpatronen
- **[Domeinmodel](architecture/DOMAIN_MODEL.md)**: Kernentiteiten en relaties
- **[Repositories](architecture/REPOSITORIES.md)**: Toegangspatronen voor gegevens (Repository, UoW, Specificatie)
- **[Observers](architecture/OBSERVERS.md)**: Event-gestuurd Observer-patroon
- **[Middleware](architecture/MIDDLEWARE.md)**: Aangepaste middleware-componenten
- **[Extensies](architecture/EXTENSIONS.md)**: DI-registratie extensiemethoden
- **[GERDA AI-modules](architecture/gerda-ai/GERDA_MODULES.md)**: G.E.R.D.A. AI-pipeline documentatie

### Gidsen
- **[Ontwikkeling](guides/DEVELOPMENT.md)**: Lokale ontwikkelingsinstellingen en workflow
- **[Testen](guides/TESTING.md)**: Testprojectstructuur en patronen
- **[Configuratie](guides/CONFIGURATION.md)**: YAML/JSON configuratiegids
- **[Probleemoplossing](guides/TROUBLESHOOTING.md)**: Veelvoorkomende problemen en oplossingen
- **[Gegevens Seeding](guides/DATA_SEEDING.md)**: Configuratie van database-seedgegevens

### Implementatie (Deployment)
- **[Fly.io](deployment/FLY_IO.md)**: Fly.io implementatiegids
- **[Docker](deployment/DOCKER_GUIDE.md)**: Docker containerisatie
- **[CI/CD](deployment/CI_CD.md)**: GitHub Actions pipeline

### Projectbeheer
- **[Roadmap](project-management/roadmap_v3.md)**: Implementatie roadmap en fasen

### Assets
- **[Screenshots & Visuals](assets/)**: UI-screenshots en presentatiemateriaal

---

## 3. Architectuursamenvatting

### Architectuur in een oogopslag

**Type:** Modulaire Monoliet met AI-Augmentatie  
**Stack:** ASP.NET Core MVC + EF Core + ML.NET

```text
Presentatie → Services → Repositories → Database
                  ↓
              Observers → GERDA AI
```

### Belangrijkste Ontwerppatronen

| Patroon | Doel | Locatie |
|---------|------|---------|
| **Observer** | Event-gestuurde meldingen | `Observers/` |
| **Repository + UoW** | Abstractie van gegevenstoegang | `Repositories/` |
| **Specificatie** | Herbruikbare queries | `Repositories/Specifications/` |
| **Strategie** | Wisselbare AI-algoritmen | `Services/GERDA/` |
| **Facade** | Orchestratie van het AI-subsysteem | `GerdaService` |
| **Factory** | Objectcreatie | `TicketFactory` |

### Service Architectuur (CQRS-lite)

| Interface | Verantwoordelijkheid |
|-----------|----------------------|
| `ITicketQueryService` | Leesbewerkingen |
| `ITicketCommandService` | Schrijfbewerkingen |
| `ITicketFactory` | Ticketcreatie |

---

## 4. Snel aan de slag voor Ontwikkelaars

1. **`Program.cs`** → DI setup
2. **`DbSeeder.cs`** → Voorbeeldgegevens
3. **`TicketService.cs`** → Bedrijfslogica
4. **`GerdaService.cs`** → AI-hub

---

Voor gedetailleerde instructies over de implementatie, zie de [deployment](deployment/FLY_IO.md) map.
Voor architectuurdetails, zie [Gedetailleerde Architectuur](architecture/DETAILED.md).
