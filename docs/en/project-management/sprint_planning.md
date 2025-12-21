# Sprint Planning - Ticket Masala

Sprintduur: 2 weken
Projecteinddatum: 21 december 2025
Huidige Sprint: Sprint 6 (8 dec - 21 dec)
Laatst bijgewerkt: 9 december 2025

---

## 1. Team en Rolverdeling

### Teamprofielen

| Lid | Achtergrond | Ervaring |
|-----|-------------|----------|
| **Juan** | Brussel Fiscaliteit | AI/Data Science/BI in Python, SQL. Basis webdev |
| **Maarten** | Fullstack webontwikkelaar | C# backend, TypeScript frontend, SQL |
| **Charlotte** | Risk Specialist | Projectmanagement. Basis webdev, Javascript, SQL |
| **Wito** | Policy Officer | Java, .NET, PHP, Node.js met RDBMS |

### Teamstructuur

| Team | Leden | Focus |
|------|-------|-------|
| **Backend** | Maarten, Wito | API, logica, authenticatie, database |
| **Frontend** | Charlotte | Interface, flows, dashboards |
| **AI/ML** | Juan | GERDA (Grouping, Estimating, Ranking, Dispatching, Anticipation) |

---

## 2. Projectstatus

### Afgerond (Opsomming)

- Rolgebaseerde Authenticatie (Sprint 1-2)
- Gebruikersbeheer & Klantenbeheer (Sprint 2)
- Project & Ticket CRUD (Sprint 1-3)
- REST API & Deployment (Sprint 3)
- Zoeken & Filteren (Sprint 4)
- ML.NET Integratie (Sprint 4)
- Reacties, Batch, Notificaties (Sprint 5)
- Manager Dashboard & Audittrail (Sprint 5)
- UI Vertalingen (Sprint 5)

### Resterend Werk (Sprint 6 Focus)

De focus ligt nu op het klantenportaal en de finale afwerking.

| Prioriteit | Onderdeel | Status | Eigenaar | Opmerkingen |
|------------|-----------|--------|----------|-------------|
| **Hoog** | Klantenportaal: Data isolatie | Bezig | Maarten | Klant ziet enkel eigen data |
| **Hoog** | Klantenportaal: Tickets aanmaken | Te doen | Wito | Self-service functionaliteit |
| **Hoog** | Documentatie & Demo | Te doen | Allen | Slides, script, readme |
| **Middel** | Workflow statussen | Te doen | Wito | Configureren overgangen |
| **Middel** | Dashboard statistieken | Te doen | Charlotte | Widget op startpagina |
| **Middel** | Parent-Child UI | Deels | Charlotte | Backend gereed |
| **Laag** | GERDA Visualisaties | Te doen | Juan | Backlog & Forecast tonen |

---

## 3. Sprint 6 Detail (8 dec - 21 dec)

Doel: Klantenportaal, workflow afwerking en oplevering.
Dit is de laatste sprint voor de deadline.

### Takenlijst

| ID | Omschrijving | Punten | Wie | Status |
|----|--------------|--------|-----|--------|
| S6-1 | Data isolatie klanten | 5 | Maarten | Bezig |
| S6-2 | Ticket aanmaken (klant) | 3 | Wito | Te doen |
| S6-3 | Dashboard statistieken | 3 | Charlotte | Te doen |
| S6-4 | Parent-child UI | 2 | Charlotte | Te doen |
| S6-5 | Workflow statussen | 3 | Wito | Te doen |
| S6-6 | GERDA Metrics (Visualisatie) | 2 | Juan | Te doen |
| S6-7 | Bugfixes en afwerking | 5 | Allen | Bezig |
| S6-8 | Documentatie en demo | 3 | Allen | Te doen |

### Prioriteiten

1. **Klantenportaal** (Cruciaal voor demo)
2. **Workflow** (Productie gereedheid)
3. **UI Verbeteringen** (Presentatie)
4. **Documentatie** (Overdracht)

---

## 4. Werkwijze

### Git Strategie

- **main**: Productie code
- **develop**: Integratie
- **feature/**: Nieuwe functies
- **fix/**: Foutoplossingen

### Commit Berichten

Format: `<type>(<scope>): <omschrijving>`

Types:
- feat: Nieuwe functionaliteit
- fix: Bugfix
- docs: Documentatie
- refactor: Code verbetering
- chore: Onderhoud

---

## 5. Eindlevering (21 dec)

### Gereed
1. Productie deployment (Fly.io)
2. GERDA AI suite
3. Meertalige UI
4. Manager Dashboard
5. Reacties & Notificaties

### Nog te doen
1. Klantenportaal (Self-service)
2. Workflow configuratie
3. Volledige documentatie
4. Demo presentatie
