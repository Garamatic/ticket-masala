# Ticket Masala - Architectuursamenvatting

**Beknopte Gids** | Zie [Gedetailleerde Architectuur](DETAILED.md) voor de volledige documentatie.

---

## Architectuur in een oogopslag

**Type:** Modulaire Monoliet met AI-Augmentatie  
**Stack:** ASP.NET Core MVC + EF Core + ML.NET

```text
Presentatie → Services → Repositories → Database
                  ↓
              Observers → GERDA AI
```

---

## Universal Entity Model (UEM) Terminologie

Het systeem maakt gebruik van een **gelaagde terminologiestrategie** om de consistentie van de externe API te balanceren met de stabiliteit van de interne code.

| UEM (Publieke API) | Interne Code | Beschrijving |
|--------------------|--------------|--------------|
| **WorkItem** | `Ticket` | Individuele werkeenheid die verwerkt moet worden |
| **WorkContainer** | `Project` | Verzameling van gerelateerde werkitems |
| **WorkHandler** | `ApplicationUser` | Persoon verantwoordelijk voor het werk |

> **API-routes:** Zowel `/api/v1/tickets` als `/api/v1/workitems` worden ondersteund.
>
> **Views:** Labels zijn per domein configureerbaar via `masala_domains.yaml` → `entity_labels`.

Zie [ADR-001](ADR-001-uem-terminology.md) voor de volledige onderbouwing.

---

## Belangrijkste Ontwerppatronen

| Patroon | Doel | Locatie |
|---------|------|---------|
| **Observer** | Event-gestuurde meldingen | `Observers/` |
| **Repository + UoW** | Abstractie van gegevenstoegang | `Repositories/` |
| **Specificatie** | Herbruikbare queries | `Repositories/Specifications/` |
| **Strategie** | Wisselbare AI-algoritmen | `Services/GERDA/` |
| **Facade** | Orchestratie van het AI-subsysteem | `GerdaService` |
| **Factory** | Objectcreatie | `TicketFactory` |

---

## Gedetailleerd Ontwerp

### Modulaire Monoliet

Het systeem is ontworpen als een modulaire monoliet, waarbij eenvoud en schaalbaarheid in balans zijn. Elke module is op zichzelf staand, met duidelijke grenzen en verantwoordelijkheden.

### Belangrijkste Modules

1. **Presentatielaag**: Beheert gebruikersinteracties.
2. **Servicelaag**: Bevat de bedrijfslogica.
3. **Repositorylaag**: Beheert de toegang tot gegevens.
4. **AI-laag**: Implementeert de GERDA-strategieën.

---

## GERDA AI-modules

| Letter | Module | Techniek |
|--------|--------|----------|
| **G** | Grouping (Groeperen) | K-Means (spamdetectie) |
| **E** | Estimating (Inschatten) | Classificatie (inspanning) |
| **R** | Ranking (Prioriteren) | WSJF (prioriteit) |
| **D** | Dispatching (Toewijzen) | Matrix Factorization (agent matching) |
| **A** | Anticipation (Anticiperen) | Time Series (voorspelling) |

---

## Service Architectuur (CQRS-lite)

| Interface | Verantwoordelijkheid |
|-----------|----------------------|
| `ITicketQueryService` | Leesbewerkingen |
| `ITicketCommandService` | Schrijfbewerkingen |
| `ITicketFactory` | Ticketcreatie |

---

## Snel aan de slag voor Ontwikkelaars

1. **`Program.cs`** → DI-instellingen
2. **`DbSeeder.cs`** → Laadt voorbeeldgegevens vanuit `config/seed_data.json`
3. **`TicketService.cs`** → Bedrijfslogica
4. **`GerdaService.cs`** → AI-hub

---

## Belangrijke Beslissingen

| Beslissing | Onderbouwing |
|------------|--------------|
| Lokaal ML.NET | AVG-privacy, geen API-kosten |
| Modulaire Monoliet | Eenvoudiger beheer dan microservices |
| Observer-patroon | AI-verwerking vertraagt de UI niet |
| Repository-patroon | Testbaar, onafhankelijk van database |

---

*Volledige details: [Gedetailleerde Architectuur](DETAILED.md)*
