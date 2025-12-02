# Agile Sprint Planning - Ticket Masala

> **Sprint Length:** 1 week (from Sprint 4 onwards)  
> **Project End Date:** December 21, 2025  
> **Updated:** November 29, 2025

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

### Future Roles (vanaf Sprint 5+)

- **QA, Analytics & Infra:** TBD - testing, CI/CD, documentation, Agile coordination

---

## 📊 Current Project Status

### ✅ What's Done (MVP Complete)

| Feature | Status | Notes |
|---------|--------|-------|
| Role-based Authentication | ✅ Done | Admin, Employee, Customer roles |
| User Management | ✅ Done | Employee/Customer types with seeding |
| Project CRUD | ✅ Done | Create, Read, Update, Delete |
| Ticket CRUD | ✅ Done | Including Create action |
| Customer Management | ✅ Done | List and detail views |
| REST API | ✅ Done | `/api/v1/projects` with CRUD + stats |
| Deployment | ✅ Done | Fly.io with SQLite, Docker |
| UI Framework | ✅ Done | Bootstrap with green theme |

### ⚠️ Partially Done

| Feature | Current State | What's Missing |
|---------|---------------|----------------|
| Comments | `List<string>` on Ticket model | No UI to add/view comments |
| Parent-Child Tickets | Model supports hierarchy | No linking UI |
| Project Manager Assignment | Field exists | No reassignment UI |
| Ticket Workflow | Status enum exists | No state transition logic |

### ❌ Not Started

- Search & filtering (tickets, projects)
- Batch operations
- Notifications/messaging
- Document attachments
- Calendar/scheduling view
- Milestones/phases
- Analytics dashboard
- Customer portal isolation
- All AI/GERDA features

---

## 📜 Sprint History (Original Planning)

### Sprint 1 (13/10 - 26/10) ✅ Completed

- ✅ Ticketaanmaakfunctie
- ✅ Opslag, overzicht en aanpassing
- ⚠️ Zoeken en filteren (moved to later sprint)

### Sprint 2 (27/10 - 09/11) ✅ Completed

- ✅ Gebruikers (klanten en medewerkers) – role based auth
- ⚠️ Case/Projectlogica – parent en child tickets (partially done)
- ❌ API calls naar AI (moved to later sprint)

### Sprint 3 (10/11 - 23/11) ✅ Completed

- ✅ Deployment to Fly.io with SQLite
- ✅ Ticket Create functionality
- ✅ Role seeding fix for production
- ⚠️ Search & filtering (in progress)

---

## 🎯 Sprint Backlog

### Sprint 4 (Current) - Search & Filtering

**Dates:** Nov 24 - Nov 30, 2025 (Week 1)

| ID | Story | Points | Assignee | Priority |
|----|-------|--------|----------|----------|
| S4-1 | Search tickets by description | 3 | Maarten | High |
| S4-2 | Filter tickets by status | 2 | Wito | High |
| S4-3 | Improve ticket list UI | 2 | Charlotte | High |
| S4-4 | AI: ML.NET setup + masala_config.json integration | 3 | Juan | High |

**Sprint Goal:** Basic search and filtering works, GERDA foundation ready.

---

### Sprint 5 - Comments & GERDA Core

**Dates:** Dec 1 - Dec 7, 2025 (Week 2)

| ID | Story | Points | Assignee | Priority |
|----|-------|--------|----------|----------|
| S5-1 | Add/view comments on tickets | 5 | Maarten/Charlotte | High |
| S5-2 | Batch update ticket status | 3 | Wito | Medium |
| S5-3 | GERDA-G: Spam detection & ticket grouping | 5 | Juan | High |
| S5-4 | GERDA-E: Complexity estimation (Fibonacci points) | 3 | Juan | High |

**Sprint Goal:** Comments working, GERDA groups spam tickets and estimates complexity.

---

### Sprint 6 - Customer Portal & GERDA Intelligence

**Dates:** Dec 8 - Dec 14, 2025 (Week 3)

| ID | Story | Points | Assignee | Priority |
|----|-------|--------|----------|----------|
| S6-1 | Customer sees only their data | 5 | Maarten | High |
| S6-2 | Customer can create tickets | 2 | Wito | High |
| S6-3 | Dashboard with ticket stats | 5 | Charlotte | Medium |
| S6-4 | GERDA-R: WSJF Priority ranking algorithm | 5 | Juan | High |
| S6-5 | GERDA-D: Agent-ticket dispatching (ML.NET recommendation) | 5 | Juan | High |

**Sprint Goal:** Customer portal isolated, GERDA ranks and dispatches tickets.

---

### Sprint 7 (Final) - Polish & GERDA Anticipation

**Dates:** Dec 15 - Dec 21, 2025 (Week 4)

| ID | Story | Points | Assignee | Priority |
|----|-------|--------|----------|----------|
| S7-1 | Ticket workflow state transitions | 3 | Wito | High |
| S7-2 | Link parent-child tickets UI | 3 | Maarten/Charlotte | Medium |
| S7-3 | GERDA-A: Capacity forecasting (Time Series SSA) | 5 | Juan | Medium |
| S7-4 | GERDA Dashboard: Director alerts & insights | 3 | Juan/Charlotte | Medium |
| S7-5 | Bug fixes & final polish | 5 | All | High |
| S7-6 | Documentation & demo prep | 3 | All | High |

**Sprint Goal:** Production-ready with full GERDA intelligence suite.

---

## 🌿 Git Branching Strategy

### Branch Structure

```text
main (production)
  └── develop (integration)
        ├── feature/search-tickets
        ├── feature/comments-ui
        ├── feature/customer-isolation
        └── fix/login-validation
```

### Branch Naming Convention

| Type | Pattern | Example |
|------|---------|---------|
| Feature | `feature/<description>` | `feature/search-tickets` |
| Bug Fix | `fix/<description>` | `fix/login-redirect` |
| Hotfix | `hotfix/<description>` | `hotfix/db-connection` |
| Release | `release/<version>` | `release/1.2.0` |

### Workflow

1. **Start a feature:**

   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/search-tickets
   ```

2. **Work on feature:**

   ```bash
   # Make changes
   git add .
   git commit -m "feat: add ticket search endpoint"
   ```

3. **Push and create PR:**

   ```bash
   git push -u origin feature/search-tickets
   # Create Pull Request on GitHub: feature/search-tickets → develop
   ```

4. **After PR approval:**

   ```bash
   # Merge via GitHub PR (squash or merge commit)
   # Delete feature branch
   ```

5. **Release to production:**

   ```bash
   git checkout main
   git merge develop
   git tag v1.2.0
   git push origin main --tags
   fly deploy
   ```

---

## 📝 Commit Message Convention

Use [Conventional Commits](https://www.conventionalcommits.org/):

```text
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types

| Type | Description |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `style` | Formatting, no code change |
| `refactor` | Code restructuring |
| `test` | Adding tests |
| `chore` | Maintenance tasks |

### Examples

```bash
feat(tickets): add search by description
fix(auth): resolve login redirect loop
docs(readme): update deployment instructions
refactor(api): extract validation to middleware
```

---

## 🔄 Pull Request Process

1. **Create PR** with descriptive title following commit convention
2. **Fill template:**
   - What does this PR do?
   - How to test?
   - Screenshots (if UI changes)
3. **Request review** from at least 1 team member
4. **Address feedback** and push updates
5. **Squash and merge** when approved
6. **Delete branch** after merge

### PR Checklist

- [ ] Code compiles without errors
- [ ] Tests pass locally
- [ ] UI changes are responsive
- [ ] No console errors
- [ ] Database migrations included if needed

---

## 🚀 Deployment Workflow

### Development

```bash
dotnet run  # Uses SQL Server from appsettings.Development.json
```

### Staging (Optional)

```bash
fly deploy --config fly.staging.toml
```

### Production

```bash
# Ensure on main branch with all changes merged
git checkout main
git pull origin main
fly deploy
```

### Rollback

```bash
fly releases list
fly deploy --image <previous-image-ref>
```

---

## 👥 Team Assignments

| Member | Focus Area | Sprint 4 Tasks (This Week) |
|--------|------------|----------------------------|
| **Maarten** | Backend/API | S4-1: Search endpoint |
| **Wito** | Backend/DB | S4-2: Status filtering |
| **Charlotte** | Frontend/UI | S4-3: Ticket list UI |
| **Juan** | AI/ML | S4-4: ML.NET + masala_config integration |

---

## 🎯 Definition of Done

A story is **Done** when:

- [ ] Code is written and compiles
- [ ] Unit tests pass
- [ ] Code reviewed and approved
- [ ] Merged to develop branch
- [ ] Deployed to staging (if applicable)
- [ ] Acceptance criteria met
- [ ] Documentation updated
