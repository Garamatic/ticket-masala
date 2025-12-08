# Agile Sprint Planning - Ticket Masala

> **Sprintduur:** 2 weken  
> **Projecteinddatum:** 21 december 2025  
> **Huidige Sprint:** Sprint 6 (8 dec - 21 dec)  
> **Laatst bijgewerkt:** 8 december 2025

---

## 👥 Team & Rolverdeling

### Teamprofielen

| Lid | Achtergrond | Ervaring |
|-----|-------------|----------|
| **Juan** | Brussel Fiscaliteit (legal-econoom-automatisering) | AI/Data Science/BI in Python, SQL, QLIK/PowerBI. Basiskennis webdev in Javascript |
| **Maarten** | Fullstack webontwikkelaar | C# backend, TypeScript frontend, SQL. Game-applicatieproject afgerond |
| **Charlotte** | Risk Specialist bij Infrabel | Projectmanagement, risicobeheer, rapportering. Basiskennis webdev, Javascript, SQL. Allergie App project |
| **Wito** | Policy Officer VUB (Onderzoek) | Biomedische wetenschappen, strategieconsulting. Java, .NET, PHP, Node.js met RDBMS |

### Teamstructuur

| Team | Leden | Focus |
|------|-------|-------|
| **Backend** | Maarten, Wito | API, bedrijfslogica, authenticatie, REST endpoints, DB schema |
| **Frontend** | Charlotte | Gebruikersinterface, gebruikersflows, dashboards |
| **AI/ML** | Juan | GERDA (Grouping, Estimating, Ranking, Dispatching, Anticipation) via ML.NET |

---

## 📊 Huidige Projectstatus

### ✅ Afgerond (Alle Sprints)

| Functionaliteit | Status | Sprint |
|-----------------|--------|--------|
| Rolgebaseerde Authenticatie | ✅ Klaar | Sprint 1-2 |
| Gebruikersbeheer | ✅ Klaar | Sprint 2 |
| Project CRUD | ✅ Klaar | Sprint 1-2 |
| Ticket CRUD | ✅ Klaar | Sprint 3 |
| Klantenbeheer | ✅ Klaar | Sprint 2 |
| REST API | ✅ Klaar | Sprint 3 |
| Deployment (Fly.io) | ✅ Klaar | Sprint 3 |
| UI Framework | ✅ Klaar | Sprint 3 |
| Zoeken & Filteren | ✅ Klaar | Sprint 4 |
| ML.NET + masala_config integratie | ✅ Klaar | Sprint 4 |
| Reactiesysteem | ✅ Klaar | Sprint 5 |
| Batchbewerkingen | ✅ Klaar | Sprint 5 |
| Volledige GERDA Suite (G, E, R, D, A) | ✅ Klaar | Sprint 5 |
| Manager Dashboard (Team, Capaciteit, Dispatch) | ✅ Klaar | Sprint 5 |
| Notificatiesysteem | ✅ Klaar | Sprint 5 |
| Audittrail | ✅ Klaar | Sprint 5 |
| UI Vertalingen (EN/FR/NL) | ✅ Klaar | Sprint 5 |
| Architectuur Refactoring (CQRS, Factory) | ✅ Klaar | Sprint 5 |
| Externe Ticket Indienen API | ✅ Klaar | Sprint 5 |
| Projectsjablonen | ✅ Klaar | Sprint 5 |
| Parent-Child Ticket Koppeling (Backend) | ✅ Klaar | Sprint 5 |

### 🚧 Resterend Werk (Sprint 6)

| Functionaliteit | Status | Opmerkingen |
|-----------------|--------|-------------|
| Klantenportaal Isolatie | ⏳ Bezig | Klant ziet alleen eigen gegevens |
| Klant Ticket Aanmaken | 🔲 Nog niet gestart | Self-service ticketportaal |
| Dashboard Ticket Statistieken Widget | 🔲 Nog niet gestart | Homepage statistieken |
| Parent-Child Tickets UI | ⏳ Gedeeltelijk | Backend klaar, UI verbeteringen nodig |
| Ticket Workflow Statusovergangen | 🔲 Nog niet gestart | Configureerbare workflowstatussen |
| Bugfixes & Polish | 🔲 Nog niet gestart | Finale tests |
| Demo Voorbereiding | 🔲 Nog niet gestart | Presentatie & documentatie |

---

## 📜 Sprintgeschiedenis

### Sprint 1 (13/10 - 26/10) ✅ Afgerond

- ✅ Ticket aanmaken, opslaan, overzicht, wijzigen

### Sprint 2 (27/10 - 09/11) ✅ Afgerond

- ✅ Gebruikersbeheer (klanten en medewerkers)
- ✅ Rolgebaseerde authenticatie

### Sprint 3 (10/11 - 23/11) ✅ Afgerond

- ✅ Deployment naar Fly.io met SQLite
- ✅ Ticket Create functionaliteit
- ✅ Role seeding fix voor productie

### Sprint 4 (24 nov - 30 nov) ✅ Afgerond

- ✅ Tickets zoeken op beschrijving
- ✅ Tickets filteren op status/type/agent/klant
- ✅ Verbeterde ticketlijst UI
- ✅ ML.NET setup + masala_config.json integratie

### Sprint 5 (1 dec - 7 dec) ✅ Afgerond

- ✅ Reactiesysteem
- ✅ Batchbewerkingen
- ✅ Volledige GERDA Suite (G, E, R, D, A)
- ✅ Manager Dashboard (TeamDashboard, CapacityForecast, DispatchBacklog)
- ✅ Notificatiesysteem (Observer pattern)
- ✅ Audittrail
- ✅ UI Vertalingen (EN/FR/NL)
- ✅ Architectuur Refactoring (CQRS-lite, Factory Pattern)
- ✅ Documentatie Consolidatie
- ✅ Externe Ticket API (`POST /api/v1/tickets/external`)
- ✅ Landscaping Demo Integratie
- ✅ Projectsjablonen Module
- ✅ Parent-Child Ticket Koppeling (Backend + Detail View)

---

## 🔄 Sprint 6 - Huidig (8 dec - 21 dec)

**Sprintdoel:** Klantenportaal, workflow polish, en productie-klare oplevering.

> [!NOTE]
> Dit is de **laatste sprint** vóór projectoplevering op 21 december.

| ID | Verhaal | Punten | Uitvoerder | Status | Prioriteit |
|----|---------|--------|------------|--------|------------|
| S6-1 | Klant ziet alleen eigen gegevens | 5 | Maarten | ⏳ Bezig | Hoog |
| S6-2 | Klant kan tickets aanmaken (self-service) | 3 | Wito | 🔲 Nog niet gestart | Hoog |
| S6-3 | Dashboard met ticket statistieken widget | 3 | Charlotte | 🔲 Nog niet gestart | Gemiddeld |
| S6-4 | Parent-child tickets UI polish | 2 | Charlotte | 🔲 Nog niet gestart | Gemiddeld |
| S6-5 | Ticket workflow statusovergangen | 3 | Wito | 🔲 Nog niet gestart | Gemiddeld |
| S6-6 | GERDA Dashboard verbeteringen | 2 | Juan | 🔲 Nog niet gestart | Laag |
| S6-7 | Bugfixes & finale polish | 5 | Allen | 🔲 Nog niet gestart | Hoog |
| S6-8 | Documentatie & demo voorbereiding | 3 | Allen | 🔲 Nog niet gestart | Hoog |

### Sprint 6 Prioriteiten

1. **Klantenportaal (S6-1, S6-2)** - Cruciaal voor demo
2. **Workflow Polish (S6-5, S6-7)** - Productie gereedheid
3. **UI Verbeteringen (S6-3, S6-4, S6-6)** - Demo aantrekkingskracht
4. **Documentatie (S6-8)** - Overdracht gereedheid

---

## 🌿 Git Branching Strategie

```text
main (productie)
  └── develop (integratie)
        ├── feature/<beschrijving>
        ├── fix/<beschrijving>
        └── hotfix/<beschrijving>
```

---

## 📝 Commit Conventie

```text
<type>(<scope>): <beschrijving>
```

| Type | Gebruik |
|------|---------|
| `feat` | Nieuwe functionaliteit |
| `fix` | Bugfix |
| `docs` | Documentatie |
| `refactor` | Code herstructurering |
| `chore` | Onderhoud |

---

## 🎯 Definition of Done

- [x] Code compileert zonder fouten
- [x] Code gereviewed en goedgekeurd
- [x] Gemerged naar develop branch
- [x] Documentatie bijgewerkt

---

## 🚀 Finale Deliverables (21 dec)

### Kernfunctionaliteiten (✅ Afgerond)

1. **Productie deployment** op Fly.io met SQLite
2. **Volledige GERDA AI suite** operationeel (Grouping, Estimating, Ranking, Dispatching, Anticipation)
3. **Meertalige UI** (EN/FR/NL)
4. **Manager Dashboard** met Team, Capaciteit, en Dispatch views
5. **Projectsjablonen** module
6. **Reacties & Notificaties** systeem

### Nog af te ronden (Sprint 6)

1. **Klantenportaal** voor self-service ticket aanmaken
2. **Workflow Statusovergangen** configuratie
3. **Architectuur documentatie** up-to-date
4. **Demo presentatie** gereed
