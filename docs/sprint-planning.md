# Agile Sprint Planning - Ticket Masala

> **Sprint Length:** 1 week  
> **Project End Date:** December 21, 2025  
> **Last Updated:** December 7, 2025

---

## 👥 Team & Rolverdeling

### Team Profiles

| Member | Background | Experience |
|--------|------------|------------|
| **Juan** | Brussel Fiscaliteit (legal-econoom-automatisering) | AI/Data Science/BI in Python, SQL, QLIK/PowerBI. Basic webdev in Javascript |
| **Maarten** | Fullstack web developer | C# backend, TypeScript frontend, SQL. Completed game application project |
| **Charlotte** | Risk Specialist bij Infrabel | Projectmanagement, risicobeheer, rapportering. Basic webdev, Javascript, SQL. Allergie App project |
| **Wito** | Policy Officer VUB (Research) | Biomedische wetenschappen, strategy consulting. Java, .NET, PHP, Node.js with RDBMS |

### Team Structure

| Team | Members | Focus |
|------|---------|-------|
| **Backend** | Maarten, Wito | API, business logic, authentication, REST endpoints, DB schema |
| **Frontend** | Charlotte | User interface, user flows, dashboards |
| **AI/ML** | Juan | GERDA (Grouping, Estimating, Ranking, Dispatching, Anticipation) via ML.NET |

---

## 📊 Current Project Status

### ✅ Completed (MVP+)

| Feature | Status | Sprint |
|---------|--------|--------|
| Role-based Authentication | ✅ Done | Sprint 1-2 |
| User Management | ✅ Done | Sprint 2 |
| Project CRUD | ✅ Done | Sprint 1-2 |
| Ticket CRUD | ✅ Done | Sprint 3 |
| Customer Management | ✅ Done | Sprint 2 |
| REST API | ✅ Done | Sprint 3 |
| Deployment (Fly.io) | ✅ Done | Sprint 3 |
| UI Framework | ✅ Done | Sprint 3 |
| Search & Filtering | ✅ Done | Sprint 4 |
| ML.NET + masala_config integration | ✅ Done | Sprint 4 |

### ✅ Completed This Sprint (Sprint 5)

| Feature | Status |
|---------|--------|
| Comments System | ✅ Done |
| Batch Operations | ✅ Done |
| GERDA-G: Spam Detection (v2 Grouping) | ✅ Done |
| GERDA-E: Effort Estimation | ✅ Done |
| GERDA-R: Priority Ranking (Rule Engine) | ✅ Done |
| GERDA-D: Agent Dispatching (FTS5) | ✅ Done |
| GERDA-A: Forecasting | ✅ Done |
| Manager Dashboard | ✅ Done |
| Notification System | ✅ Done |
| Audit Trail | ✅ Done |
| UI Translations (EN/FR/NL) | ✅ Done |
| Language Switcher Fix | ✅ Done |
| Architecture Refactoring (CQRS, Factory) | ✅ Done |
| Documentation Consolidation | ✅ Done |

---

## 📜 Sprint History

### Sprint 1 (13/10 - 26/10) ✅ Completed

- ✅ Ticket creation, storage, overview, modification

### Sprint 2 (27/10 - 09/11) ✅ Completed

- ✅ User management (customers and employees)
- ✅ Role-based authentication

### Sprint 3 (10/11 - 23/11) ✅ Completed

- ✅ Deployment to Fly.io with SQLite
- ✅ Ticket Create functionality
- ✅ Role seeding fix for production

### Sprint 4 (Nov 24 - Nov 30) ✅ Completed

- ✅ Search tickets by description
- ✅ Filter tickets by status/type/agent/customer
- ✅ Improved ticket list UI
- ✅ ML.NET setup + masala_config.json integration

---

## 🔄 Sprint 5 - Current (Dec 1 - Dec 7)

**Sprint Goal:** Comments working, GERDA intelligence complete, architecture refined.

| ID | Story | Status | Assignee |
|----|-------|--------|----------|
| S5-1 | Add/view comments on tickets | ✅ Done | Backend |
| S5-2 | Batch update ticket status | ✅ Done | Backend |
| S5-3 | GERDA-G: Spam detection & grouping | ✅ Done | Juan |
| S5-4 | GERDA-E: Complexity estimation | ✅ Done | Juan |
| S5-5 | GERDA-R: WSJF Priority ranking | ✅ Done | Juan |
| S5-6 | GERDA-D: Agent dispatching | ✅ Done | Juan |
| S5-7 | GERDA-A: Capacity forecasting | ✅ Done | Juan |
| S5-8 | UI Translations (EN/FR/NL) | ✅ Done | Juan |
| S5-9 | Architecture refactoring (CQRS, Factory) | ✅ Done | Juan |
| S5-10 | Documentation consolidation | ✅ Done | Juan |

### Architecture Improvements (This Sprint)

| Improvement | Status |
|-------------|--------|
| CQRS-lite (ITicketQueryService, ITicketCommandService) | ✅ Done |
| Factory Pattern (ITicketFactory, TicketFactory) | ✅ Done |
| Dead Code Cleanup (ApplicationUserManager, LocalCache) | ✅ Done |

---

## 📋 Sprint 6 (Dec 8 - Dec 14)

**Sprint Goal:** Customer portal isolation, dashboard enhancements.

| ID | Story | Points | Assignee | Priority |
|----|-------|--------|----------|----------|
| S6-1 | Customer sees only their data | 5 | Maarten | High |
| S6-2 | Customer can create tickets | 2 | Wito | High |
| S6-3 | Dashboard with ticket stats | 5 | Charlotte | Medium |
| S6-4 | Parent-child tickets linking UI | 3 | Maarten/Charlotte | Medium |

---

## 📋 Sprint 7 - Final (Dec 15 - Dec 21)

**Sprint Goal:** Production-ready with polish and demo preparation.

| ID | Story | Points | Assignee | Priority |
|----|-------|--------|----------|----------|
| S7-1 | Ticket workflow state transitions | 3 | Wito | High |
| S7-2 | GERDA Dashboard enhancements | 3 | Juan/Charlotte | Medium |
| S7-3 | Bug fixes & final polish | 5 | All | High |
| S7-4 | Documentation & demo prep | 3 | All | High |

---

## 🌿 Git Branching Strategy

```text
main (production)
  └── develop (integration)
        ├── feature/<description>
        ├── fix/<description>
        └── hotfix/<description>
```

---

## 📝 Commit Convention

```text
<type>(<scope>): <description>
```

| Type | Use |
|------|-----|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation |
| `refactor` | Code restructuring |
| `chore` | Maintenance |

---

## 🎯 Definition of Done

- [x] Code compiles without errors
- [x] Code reviewed and approved
- [x] Merged to develop branch
- [x] Documentation updated

---

## 🚀 Final Deliverables (Dec 21)

1. **Production deployment** on Fly.io
2. **Full GERDA suite** operational
3. **Multi-language UI** (EN/FR/NL)
4. **Architecture documentation** up to date
5. **Demo presentation** ready
