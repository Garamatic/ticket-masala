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

- **Sprint 1: Foundation** (Core CRUD & Grid)
- **Sprint 2: Governance** (RBAC & User Mgmt)
- **Sprint 3: Connectivity** (REST API & Cloud)
- **Sprint 4: Intelligence** (FTS5 & ML.NET)
- **Sprint 5: Operations** (Batch & Dashboards)
- **Sprint 6: Oplevering** (Portals & AI)
- **Sprint 7: Ecosystem** (Gatekeeper & DevOps)

### Resterend Werk (Sprint 7 Focus: Ecosystem & Scale)

Na de initiële deadline van 21 december verschuift de focus naar systeem-robuustheid, multi-domain schaalbaarheid en devops automatisering.

| Prioriteit | Onderdeel | Status | Eigenaar | Opmerkingen |
|------------|-----------|--------|----------|-------------|
| **Hoog** | **Configuration Versioning** | Afgerond | Juan | Onveranderlijke config snapshots |
| **Hoog** | **Ingestion Gatekeeper** | Afgerond | Juan | Schaalbare webhook-ingestie API |
| **Middel** | **UI Branding & Domains** | Afgerond | Juan | Dynamische thema's per domein |
| **Middel** | **CI/CD Automatisering** | Afgerond | Juan | Docker builds & GitHub Actions |
| **Middel** | **Admin Panel** | Afgerond | Juan | Beheer van rollen, data & config |

---

## 3. Sprint History

### Sprint 1: Foundation (29 Sep - 12 Okt)
*Focus: Core infrastructure and basic ticket lifecycle.*

- **Oplevering:** Functioneel systeem met basis CRUD voor Projecten en Tickets.
- **Taken:**
    - [x] Initial project setup (.NET 8 MVC)
    - [x] Database schema design & SQLite initialization
    - [x] Unified Entity Model (UEM) terminology mapping
    - [x] Basic Ticket & Project creation flows
    - [x] Sidebar layout and navigation structure

### Sprint 2: Identity & Governance (13 Okt - 26 Okt)
*Focus: Security, RBAC and user lifecycle.*

- **Oplevering:** Beveiligd platform met rol-gebaseerde toegang.
- **Taken:**
    - [x] Identity integration (Admin, Employee, Customer roles)
    - [x] User management dashboard for Admins
    - [x] Customer management and isolation foundations
    - [x] Access control decorators on controllers
    - [x] Password reset and account lock-out logic

### Sprint 3: Connectivity (27 Okt - 9 Nov)
*Focus: API foundations and early cloud presence.*

- **Oplevering:** Publieke API en geautomatiseerde deployment pipeline.
- **Taken:**
    - [x] REST API endpoints for Ticket integration
    - [x] Swagger/OpenAPI documentation (Swashbuckle)
    - [x] Initial Fly.io deployment config
    - [x] YAML-based Domain Configuration loader (v1)
    - [x] Structured Logging (Serilog) and Correlation IDs

### Sprint 4: Intelligence (10 Nov - 23 Nov)
*Focus: Search, AI classification and prioritization.*

- **Oplevering:** Zoekfunctionaliteit en eerste GERDA AI componenten.
- **Taken:**
    - [x] Full-Text Search (SQLite FTS5) integration
    - [x] ML.NET integration for automatic ticket classification
    - [x] GERDA Priority scoring (WSJF implementation)
    - [x] Advanced filtering UI (Status, Priority, Assignee)
    - [x] Model Persistence service (Save/Load ML models)

### Sprint 5: Operations (24 Nov - 7 Dec)
*Focus: Collaboration and business monitoring.*

- **Oplevering:** Volledige operationele suite met audits en dashboards.
- **Taken:**
    - [x] Ticket Comments & Internal Notes
    - [x] Batch Operations (Bulk Assignments & Closures)
    - [x] Manager Dashboard (Stats widgets & Trends)
    - [x] Centralized Audit Trail (History tracking)
    - [x] Multi-language UI support (i18n)

### Sprint 6: Oplevering (8 Dec - 21 Dec)
*Focus: Klantenportaal en finale deadline.*

- **Oplevering:** Het systeem is succesvol gedemonstreerd met functionele klantenportalen en GERDA AI routing.
- **Taken:**
    - [x] Klantenportaal: Data isolatie & Invoer
    - [x] Workflow status configuratie (Expression Trees)
    - [x] GERDA Metrics & Performance Dashboards
    - [x] Demo scenarios & Presentation prep

---

## 4. Current Planning: Sprint 7 (22 Dec - 4 Jan) 
*Doel: Schaalbaarheid, Branding en Ecosysteem.*

| ID | Omschrijving | Punten | Status |
|----|--------------|--------|--------|
| S7-1 | Config Versioning (SHA256 & DB Snapshots) | 5 | |
| S7-2 | Scalable Ingestion (Gatekeeper API + Scriban) | 8 | |
| S7-3 | Multi-Domain UI (Labels, Icons, CSS Themes) | 5 | |
| S7-4 | DevOps: Chiseled Docker Images & GHA | 3 | |
| S7-5 | Admin Readiness (Access, Roles, Data flows) | 3 | |
| S7-6 | Monitoring: Prometheus & Grafana stack | 5 | 🚧 |

---

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
