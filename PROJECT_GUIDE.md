# Integration Project: Quick Start Guide

**Project**: Ticket Masala for Shiftfestival Event Management  
**Status**: Ready for Phase 2 Implementation  
**Team**: See role assignments below  
**Timeline**: ~8 weeks (4 weeks core, 2-3 weeks external integration, 1 week testing/polish, 1-2 weeks demo preparation)

---

## Document Map

Use these in order:

1. **THIS FILE**: Overview & how to navigate
2. **[INTEGRATION_BACKLOG.md](INTEGRATION_BACKLOG.md)** → Detailed tasks, acceptance criteria, priorities
3. **[TECHNICAL_SPECIFICATION.md](TECHNICAL_SPECIFICATION.md)** → Deep-dive architecture, code patterns, schema design
4. **[spec-for-integration.md](spec-for-integration.md)** → Initial Masala Bridge concept (reference)
5. **[integration-project-task.md](integration-project-task.md)** → Course requirements (original brief)

---

## Architecture at a Glance

```
┌─ EXTERNAL SYSTEMS ─────────────────────────────┐
│ Drupal (registration) ↔ Odoo (bar) ↔ ... ↔ ... │
└─────────────────────────┬──────────────────────┘
                          │ (async, decoupled)
                    RabbitMQ Queues
                          │
┌─────────────────────────┴──────────────────────┐
│     TICKET MASALA (Our System)                 │
│                                                 │
│  Ingressors (Receivers) ← RabbitMQ messages   │
│         ↓                                       │
│  GERDA Workflow Engine (Domain Logic)         │
│    • Observers (Auto-invoice, Mailing, CRM)   │
│    • Rules (Conflict detection)                │
│         ↓                                       │
│  SQLite Database (WAL mode for speed)         │
│    • Attendees, Orders, Sessions, Invoices    │
│         ↓                                       │
│  Egressors (Senders) → RabbitMQ messages ↔   │
│         ↓                                       │
│  Admin Dashboards & APIs                      │
│    • REST (CRUD), WebSocket (realtime)        │
│    • MCP (AI queries), IoT (badge scanner)    │
└─────────────────────────────────────────────────┘
```

**Key Concept**: Masala = **Bridge** between external systems. No modifications to core Ticket Masala engine—just adapters at the edges.

---

## Phase Breakdown

### Phase 1: Foundation (Weeks 1-2)
**Goal**: Systems can talk to each other

**Key Stories**:
- 1.1: RabbitMQ setup
- 2.1-2.3: YAML configuration + SQLite schema
- 8.2-8.3: Git branches + CI pipeline
- 1.4: Heartbeat publisher

**Deliverable**: Teams can publish/consume test messages; schema auto-generated from YAML

---

### Phase 2: Core Workflows (Weeks 3-4)
**Goal**: End-to-end workflows working (registration → invoice → email)

**Key Stories**:
- 1.2: Message ingress (Drupal → Masala)
- 1.3: Message egress (Masala → FOSSBilling)
- 3.1: Facturatie observer (auto-invoice)
- 3.3: Bar ordering (high performance)
- 9.1-9.2: Unit + integration tests

**Deliverable**: Live demo: Submit registration → Invoice generated → Email sent

---

### Phase 3: Integration (Weeks 5-6)
**Goal**: All external systems connected; dashboards working

**Key Stories**:
- 7.1-7.6: Receiver/sender stubs for each team
- 4.1-4.3: Admin dashboard + health monitoring
- 5.1-5.2: IoT badge scanning
- 9.3: Load testing

**Deliverable**: Full system integration test; dashboard shows all systems online/offline

---

### Phase 4: Polish (Weeks 7-8)
**Goal**: Production-ready; handle edge cases; documentation

**Key Stories**:
- 3.2: Speaker delay cascading
- 3.5: Conflicting data resolution
- 6.1-6.2: AI queries, rule violations
- 10.1-10.4: Documentation + runbooks
- 8.4-8.5: Full CI/CD + secrets

**Deliverable**: Live event runs smoothly; admins trained; project documented

---

## Role Assignments

### Tech Lead (Integration Architect)
**Owns**: Overall system design, RabbitMQ bridge, GERDA configuration

**Key Tasks**:
- Define message contracts for all queues (STORY 1.1-1.3, 7.1-7.6)
- Design observer patterns (STORY 3.1-3.5)
- Set up Git workflow & branching (STORY 8.2)
- Coordinate with Project Manager on team communication

**Deliverable**: INTEGRATION_BACKLOG.md + TECHNICAL_SPECIFICATION.md (✓ DONE!)

---

### Backend Developer (1-2 people)
**Owns**: RabbitMQ receivers/senders, database layer, workflow engine

**Key Tasks**:
1. **Weeks 1-2**: RabbitMQ client setup, YAML loader, schema generator (STORIES 1.1, 2.1-2.3)
2. **Weeks 3-4**: Message ingress/egress, observers, bar endpoint (STORIES 1.2-1.3, 3.1-3.3)
3. **Weeks 5-6**: External integrations, error handling (STORIES 7.1-7.6, 9.1-9.2)
4. **Weeks 7-8**: Conflict resolution, AI endpoint (STORIES 3.5, 6.1-6.2)

**Key Files to Create**:
- `GatekeeperApi/Services/RabbitMqIngressService.cs`
- `GatekeeperApi/Services/RabbitMqDispatcherService.cs`
- `GatekeeperApi/Services/SchemaGeneratorService.cs`
- `GatekeeperApi/Services/FacturationObserver.cs`
- `GatekeeperApi/Models/MessageContracts/` (DTOs)

---

### DevOps/Infra Developer
**Owns**: Docker, CI/CD, Fly.io deployment, monitoring

**Key Tasks**:
1. **Weeks 1-2**: RabbitMQ Docker setup, GitHub Actions scaffolding (STORIES 1.1, 8.1-8.3)
2. **Weeks 3-4**: Enable WAL mode, optimize SQLite (STORY 2.3)
3. **Weeks 5-6**: Load testing setup, monitoring dashboard (STORY 8.1, 9.3)
4. **Weeks 7-8**: Full deployment pipeline, secrets (STORIES 8.4-8.5, 10.4)

**Key Files to Create**:
- `docker-compose.yml` (RabbitMQ + SQLite)
- `.github/workflows/test.yml`
- `.github/workflows/deploy.yml`
- `docs/RUNBOOK.md` (operations manual)

---

### Frontend Developer
**Owns**: Admin dashboard, API documentation, UX/form validation

**Key Tasks**:
1. **Weeks 3-4**: Dashboard mockups, REST API stubs
2. **Weeks 5-6**: System health dashboard (STORY 4.1)
3. **Weeks 7-8**: Admin management interface, event analytics (STORIES 4.2-4.3, 6.2)

**Key Files to Create**:
- `masala-web/src/dashboard.html` (or Vue component)
- `masala-web/src/admin/AdminPanel.vue`

---

### QA / Test Engineer
**Owns**: Testing strategy, load testing, defect tracking

**Key Tasks**:
1. **Weeks 2-4**: Unit test scaffold, testcontainers setup (STORY 9.1)
2. **Weeks 4-6**: Integration tests, message contracts (STORY 9.2)
3. **Weeks 6-7**: Load testing with k6 (STORY 9.3, 9.4)
4. **Weeks 7-8**: Final UAT, edge cases (Sad Paths)

---

### Project Manager (Course Requirement)
**Owns**: Timeline, stakeholder communication, demo preparation

**Key Responsibilities**:
- Weekly check-ins with team leads
- Bi-weekly standups with external teams (Drupal, Odoo, SendGrid, etc.)
- Escalate blockers to Tech Lead
- Prepare demo slides + coordinate demo day
- Track risks & changes from customer

**Communication Cadence**:
- Monday: Sprint planning (15 min)
- Wednesday: Mid-week sync (30 min)
- Friday: Demo / Retrospective (1 hour)

---

## Quick Start: First Day Checklist

### For Tech Lead:
- [ ] Read: TECHNICAL_SPECIFICATION.md (sections 1-4)
- [ ] Create GitHub issues from INTEGRATION_BACKLOG.md (use Epic name as label)
- [ ] Schedule kickoff meeting with team
- [ ] Assign stories to developers
- [ ] Create RabbitMQ test instance (local Docker)

### For Backend Dev #1:
- [ ] Familiarize with TECHNICAL_SPECIFICATION.md sections 2-3 (RabbitMQ & message contracts)
- [ ] Clone repo, check out dev branch
- [ ] Install RabbitMQ.Client NuGet package
- [ ] Read STORY 1.1: Create RabbitMQ.Client configuration in Program.cs
- [ ] Start with STORY 2.1: Create `config/tenants/shiftfestival/domains.yaml`

### For DevOps:
- [ ] Read: TECHNICAL_SPECIFICATION.md sections 5-7 (SQLite, IaC)
- [ ] Create docker-compose.yml with RabbitMQ + SQLite
- [ ] Set up GitHub Actions workflow template
- [ ] Document local development setup in README

### For Frontend Dev:
- [ ] Familiarize with existing masala-web structure
- [ ] Sketch admin dashboard wireframe (even on paper)
- [ ] Discuss OpenAPI spec generation with backend lead

---

## How External Teams Fit In

These teams are responsible for sending/receiving messages. We define the contracts.

| Team | System | Their Responsibility | Our Interface |
|------|--------|----------------------|---|
| **Drupal** | Registration | Send registration form submissions as JSON to our RabbitMQ | Queue: `shiftfestival.registrations.created` (JSON schema in STORY 7.1) |
| **Odoo (POS)** | Bar/Kassa | Receive badge scans, process orders, send results to our queue | Queue: `shiftfestival.orders.created` |
| **FOSSBilling** | Invoicing | Receive invoice data, create in their system | Queue: `shiftfestival.invoices.create` → Their receiver |
| **SendGrid** | Email | Receive email payloads, send to recipients | Queue: `shiftfestival.emails.send` → Their API client |
| **Office365** | Planning | Update session times when speaker delayed | Queue: `shiftfestival.sessions.updated` (we subscribe) |
| **Salesforce** | CRM | Receive attendee/company data post-event | Queue: `shiftfestival.crm.upsert` → Their API |
| **Elasticsearch** | Monitoring | Receive system heartbeats, show dashboard | Queue: `monitoring.heartbeats` → Elastic Stack |

**Our Role**: Define queue names, JSON schemas, retry logic. Provide integration guide (STORY 10.3).

---

## Key Technical Decisions

### Why RabbitMQ?
- Async messaging = loose coupling (one system down ≠ cascade failure)
- Persistence = messages survive server restart
- Routing = multiple systems can subscribe to same queue
- Built-in retries & DLQ = fault tolerance

### Why YAML Configuration?
- No C# recompilation needed (= faster iteration)
- Event organizers can customize domains without touching code
- Version control friendly (easy diffs)
- Schema auto-generates SQLite tables (less manual SQL)

### Why SQLite WAL Mode?
- Readers ≠ blocked by writers (critical for badge scans)
- Atomic transactions (order + attendee update in one tx)
- Perfect for single-server deployment (no distributed locks needed)

### Why Observer Pattern?
- Extensible: Add new workflows without modifying existing code
- Decoupled: Observers don't know about each other
- Async-friendly: Observers run in background tasks
- Testable: Mock observers in unit tests

---

## Common Pitfalls & How to Avoid

| Pitfall | Impact | Prevention |
|---------|--------|-----------|
| Hardcoding message format | Breaking change = all systems fail | Use YAML; share JSON schemas with teams |
| No idempotency checks | Duplicate invoices, double charges | Store MessageId in DB (STORY 1.2) |
| RabbitMQ down = Masala crashes | Complete outage | Use DispatchBacklog table + retry logic (STORY 9.1) |
| Bar scan latency > 100ms | Long lines, frustrated users | Use SlimSemaphore + query caching + WAL mode |
| No audit logs | Disputes, debugging impossible | Log all state changes to EventLog table |
| Database locked during reads | Dashboard freezes | WAL mode (STORY 2.3) |
| Conflicting email addresses | How do we identify attendee? | Validate uniqueness + conflict resolution (STORY 3.5) |

---

## Testing Strategy

### Unit Tests (Dev writes these)
- Per observer (FacturationObserver, etc.)
- Message transformation logic
- YAML parsing
- Configuration validation

**Tool**: xUnit + Moq  
**Coverage Target**: ≥ 70%  
**Run**: On every PR (GitHub Actions)

### Integration Tests (QA writes these)
- End-to-end workflows (registration → invoice → email)
- RabbitMQ message flow
- SQLite transactions
- Error scenarios (RabbitMQ down, DB locked, etc.)

**Tool**: Testcontainers (RabbitMQ + SQLite)  
**Run**: Nightly + before release

### Load Tests (QA runs these)
- Simulate 50 concurrent badge scans
- Verify p95 response time < 150ms
- Check for database deadlocks

**Tool**: k6 (lightweight load testing)  
**Run**: Weekly during dev, before live event

---

## Success Criteria

By end of project:

✅ **Workflow**: Registration → Invoice → Email completes in < 5 seconds

✅ **High Availability**: If Odoo/SendGrid down, Masala continues; messages queued for retry

✅ **Performance**: Badge scans processed in < 100ms (p95)

✅ **Monitoring**: Admin dashboard shows all 8 systems' uptime in real-time

✅ **Documentation**: Integration guide allows external teams to build independently

✅ **Tests**: ≥70% code coverage; all stories have unit tests

✅ **Deployment**: One-command deployment via GitHub Actions → Fly.io

✅ **Scalability**: Handles spike in registrations (10x normal load) without errors

---

## Slack/Chat Commands (Suggested)

Share these with your team for quick reference:

```
/stories → Link to INTEGRATION_BACKLOG.md
/spec → Link to TECHNICAL_SPECIFICATION.md
/boards → Link to GitHub Projects (board view)
/demo → Link to demo checklist
/contracts → Link to message JSON schemas
```

---

## Useful Links

- **GitHub**: https://github.com/Garamatic/ticket-masala
- **RabbitMQ Docs**: https://www.rabbitmq.com/documentation.html
- **SQLite WAL Mode**: https://www.sqlite.org/wal.html
- **JSON Schema Validator**: https://www.jsonschemavalidator.net/
- **.NET Reference**: https://learn.microsoft.com/en-us/dotnet/
- **Fly.io Deployment**: https://fly.io/docs/

---

## Next Steps

1. **Today**: Tech lead shares this doc with team
2. **Tomorrow**: Kickoff meeting (30 min)
   - Clarify roles
   - Assign first sprint stories (STORIES 1.1, 2.1, 8.2, 8.3)
   - Set up local dev environment
3. **This Week**: 
   - RabbitMQ running locally
   - YAML schema created
   - GitHub workflow scaffolded
4. **Next Week**: First working receiver/sender (message can roundtrip RabbitMQ)

---

## Questions for Product Owner / Customer

Before starting, clarify these with the school:

1. **Event Timing**: When is Shiftfestival 2026? (Date, hours of operation)
2. **Scale**: Expected attendee count? Peak concurrent bar scans?
3. **Data Sensitivity**: GDPR compliance needed? (EU = yes)
4. **Integration Timeline**: Do external teams have their systems ready, or are we waiting?
5. **Fallback Plan**: If RabbitMQ fails on event day, what's the manual process?
6. **Languages**: Dutch or English for admin interface?
7. **Reporting**: What data must we export post-event? (Attendee list for Salesforce, etc.)

---

## Document Change Log

| Date | Author | Change |
|------|--------|--------|
| 2026-02-18 | Tech Lead | v1.0 - Initial spec from merger of requirements + Masala Bridge pattern |

---

**Last Updated**: February 18, 2026  
**Status**: 🟢 Ready for Phase 2 (Core Implementation)
