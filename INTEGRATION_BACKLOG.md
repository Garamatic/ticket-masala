# Ticket Masala: Integration Project Backlog

**Project**: Shiftfestival Event Management Platform  
**Approach**: Masala Bridge Pattern (Event-Driven Architecture via RabbitMQ)  
**Timeline**: 3 Phases (Analysis → Implementation → Demo Weeks)  
**Status**: Phase 1/2 (Technical Specification)

---

## Overview: System Architecture

```
[External Systems] ←→ [RabbitMQ] ←→ [Masala RabbitMQ Bridge]
                                           ↓
                                  [GERDA Workflow Engine]
                                           ↓
                                  [SQLite WAL + FTS5]
```

**Key Principle**: Loose coupling via async messaging. If any external system (Odoo, FOSSBilling, SendGrid) goes down, Masala continues with graceful degradation.

---

## EPIC 1: The Connectivity Layer

### Why This Matters
Masala needs to ingest data from registration systems (Drupal), payment systems (Odoo), and dispatch to billing (FOSSBilling) and marketing (SendGrid) without blocking.

---

### STORY 1.1: RabbitMQ Client Setup & Configuration

**Priority**: 🔴 CRITICAL  
**Effort**: 2 points  
**Owner**: Infra Team / Dev

**Description**:
Set up RabbitMQ client infrastructure as foundation for all messaging.

**Acceptance Criteria**:
- [ ] RabbitMQ.Client NuGet package integrated in TicketMasala.csproj
- [ ] `appsettings.json` includes RabbitMQ connection strings (localhost for dev, env vars for prod)
- [ ] Connection pooling configured (max 10 concurrent connections)
- [ ] Unit test confirms connection to RabbitMQ (or test container)
- [ ] Graceful shutdown: channels close properly on app termination

**Sad Path Handling**:
- Connection fails → Log error, retry with exponential backoff
- Message publish fails → Queue for retry in DispatchBacklog table

---

### STORY 1.2: Inbound Message Adapter (Drupal → Registration, Odoo → Orders)

**Priority**: 🔴 CRITICAL  
**Effort**: 5 points  
**Owner**: Dev (Backend)

**Description**:
Listen to RabbitMQ queues for incoming registration and order messages. Transform XML/JSON to internal WorkItem format.

**Acceptance Criteria**:
- [ ] IHostedService `RabbitMqIngressService` created
  - Listens to queue: `shiftfestival.registrations.created`
  - Listens to queue: `shiftfestival.orders.created`
- [ ] Message schema validation
  - JSON Schema or XSD for each queue
  - Reject malformed messages → Log to ErrorQueue
- [ ] XmlToWorkItemConverter transforms external format → internal JSON
  - External: `<registration><email>X</email><sessions>["A","B"]</sessions></registration>`
  - Internal: `{"EntityType":"Registration","Email":"X","SessionRefs":["A","B"],...}`
- [ ] Idempotency: MessageId tracked in `IngestedMessages` table → No duplicates
- [ ] Unit tests with mock RabbitMQ (using testcontainers)

**Example Message Flow**:
```json
From Drupal:
{
  "id": "reg-12345",
  "timestamp": "2026-02-18T10:00:00Z",
  "type": "registration.created",
  "data": {
    "email": "john@example.com",
    "name": "John Doe",
    "company": "Acme Corp",
    "sessions": ["KEYNOTE-001", "WORKSHOP-003"]
  }
}

→ Transform to WorkItem →

{
  "entityId": "reg-12345",
  "entityType": "Attendee",
  "attributes": {
    "Email": "john@example.com",
    "FullName": "John Doe",
    "CompanyName": "Acme Corp",
    "IsCorporate": true,
    "SelectedSessions": ["KEYNOTE-001", "WORKSHOP-003"]
  },
  "status": "Registered"
}
```

---

### STORY 1.3: Outbound Message Dispatcher (Masala → FOSSBilling, SendGrid)

**Priority**: 🔴 CRITICAL  
**Effort**: 5 points  
**Owner**: Dev (Backend)

**Description**:
Send messages from Masala to external systems. Implement IMessageBus with retry logic.

**Acceptance Criteria**:
- [ ] Interface `IMessageBus` with methods:
  ```csharp
  Task PublishAsync<T>(string queueName, T message, CancellationToken ct);
  Task PublishBatchAsync<T>(string queueName, IEnumerable<T> messages, CancellationToken ct);
  ```
- [ ] Outbound queues:
  - `shiftfestival.invoices.create` → FOSSBilling
  - `shiftfestival.emails.send` → SendGrid
  - `shiftfestival.crm.upsert` → Salesforce (optional: stub)
- [ ] Retry mechanism:
  - Dead letter queue for failed publishes
  - Max 3 retries with exponential backoff
- [ ] Dependency injection setup: `services.AddScoped<IMessageBus, RabbitMqMessageBus>()`

---

### STORY 1.4: Heartbeat Provider (System Health Monitoring)

**Priority**: 🟡 HIGH  
**Effort**: 3 points  
**Owner**: Infra Team

**Description**:
Publish "I am alive" heartbeat every second to monitoring queue.

**Acceptance Criteria**:
- [ ] BackgroundService `HeartbeatPublisher` runs on startup
- [ ] Publishes to queue: `monitoring.heartbeats`
- [ ] Payload structure:
  ```json
  {
    "system": "TicketMasala",
    "instanceId": "tikmasala-prod-1",
    "timestamp": "2026-02-18T10:00:00Z",
    "status": "Healthy|Degraded|Critical",
    "metrics": {
      "cpuUsagePercent": 45.2,
      "memoryMbUsed": 512,
      "databaseLatencyMs": 5,
      "queueDepth": 12,
      "uptime": 86400
    }
  }
  ```
- [ ] Interval: 1 second (PeriodicTimer)
- [ ] If DB unavailable, status → "Degraded"; still publish
- [ ] Unit test confirms periodic execution

---

## EPIC 2: Domain Configuration & Data Model

### Why This Matters
The event structure (sessions, ticket types, badge format) must be configurable without code changes. This enables the "Shiftfestival" tenant to be customized.

---

### STORY 2.1: Tenant Configuration (Shiftfestival Profile)

**Priority**: 🟡 HIGH  
**Effort**: 3 points  
**Owner**: Tech Lead / DevOps

**Description**:
Set up YAML configuration for the Shiftfestival event tenant.

**Acceptance Criteria**:
- [ ] Create folder: `config/tenants/shiftfestival/`
- [ ] File: `config/tenants/shiftfestival/domains.yaml`
  ```yaml
  tenant: shiftfestival
  eventName: "Shiftfestival 2026"
  eventDate: "2026-04-15"
  location: "Desiderius Hogeschool, Campus XYZ"
  
  domains:
    - name: sessions
      displayName: "Workshop & Keynote Sessions"
      attributes:
        - name: Title
          dataType: string
        - name: Speaker
          dataType: string
        - name: Room
          dataType: string
        - name: StartTime
          dataType: datetime
        - name: EndTime
          dataType: datetime
        - name: Capacity
          dataType: integer
        - name: Category
          dataType: enum
          values: [KEYNOTE, WORKSHOP, NETWORKING, PANEL]
    
    - name: attendees
      displayName: "Registered Participants"
      attributes:
        - name: Email
          dataType: string
        - name: FullName
          dataType: string
        - name: Company
          dataType: string
        - name: IsCorporate
          dataType: boolean
        - name: BadgeID
          dataType: string
        - name: DietaryReq
          dataType: string
        - name: SessionSelections
          dataType: array
          refDomain: sessions
    
    - name: orders
      displayName: "Bar & Catering Orders"
      attributes:
        - name: AttendeeRef
          dataType: ref
          refDomain: attendees
        - name: ConsumableRef
          dataType: ref
          refDomain: consumables
        - name: Quantity
          dataType: integer
        - name: Status
          dataType: enum
          values: [Pending, Confirmed, Invoiced, Paid]
    
    - name: consumables
      displayName: "Bar Items & Catering"
      attributes:
        - name: Name
          dataType: string
        - name: Price
          dataType: decimal
        - name: Category
          dataType: enum
          values: [BEVERAGE, FOOD, DESSERT]
```
- [ ] File: `config/tenants/shiftfestival/workflows.yaml` (see EPIC 3)
- [ ] Load YAML at startup via TenantConfigurationService
- [ ] Validate YAML schema on load

---

### STORY 2.2: Dynamic Column Generation (SQLite Schema)

**Priority**: 🟡 HIGH  
**Effort**: 4 points  
**Owner**: Dev (Backend / Database)

**Description**:
Auto-generate SQLite tables and columns from YAML configuration without manual ALTER TABLE.

**Acceptance Criteria**:
- [ ] Service `SchemaGeneratorService` on startup:
  - Reads shiftfestival/domains.yaml
  - For each domain → Create table if not exists
  - Add columns per attributes defined
  - Migrations tracked in `_SchemaHistory` table
- [ ] Example: From domains.yaml, create:
  ```sql
  CREATE TABLE IF NOT EXISTS attendees (
    id TEXT PRIMARY KEY,
    Email TEXT,
    FullName TEXT,
    Company TEXT,
    IsCorporate BOOLEAN,
    BadgeID TEXT UNIQUE,
    DietaryReq TEXT,
    SessionSelections JSON,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME
  );
  ```
- [ ] Idempotent: Running multiple times doesn't duplicate columns
- [ ] Type mapping: YAML dataTypes → SQLite types
  - string → TEXT
  - integer → INTEGER
  - decimal → REAL
  - datetime → DATETIME
  - boolean → BOOLEAN
  - array/json → JSON
- [ ] Unit tests with in-memory SQLite

---

### STORY 2.3: Enable SQLite WAL Mode

**Priority**: 🟡 HIGH  
**Effort**: 1 point  
**Owner**: Infra / DBA

**Description**:
Enable Write-Ahead Logging for concurrent badge scans at the bar.

**Acceptance Criteria**:
- [ ] On SQLite connection open, execute: `PRAGMA journal_mode=WAL;`
- [ ] Verify: `PRAGMA journal_mode;` returns "wal"
- [ ] Configuration in `appsettings.json`:
  ```json
  "ConnectionStrings": {
    "Sqlite": "Data Source=masala.db;Journal Mode=Wal;"
  }
  ```
- [ ] Stress test: Simulate 20 concurrent badge scans → No deadlocks
- [ ] Document: WAL enables readers while writes happen; cleanup via checkpoint timer

---

## EPIC 3: Workflow Automation & Business Rules

### Why This Matters
When a registration comes in or a bar order is confirmed, Masala must automatically trigger the next steps (invoice, email, CRM update) via GERDA workflow engine and observers.

---

### STORY 3.1: The "Facturatie" Observer Pattern

**Priority**: 🔴 CRITICAL  
**Effort**: 8 points  
**Owner**: Dev (Backend) + Tech Lead

**Description**:
Auto-generate invoices for corporate participants when order is confirmed.

**Acceptance Criteria**:
- [ ] Observer pattern: `IWorkItemObserver` interface
  ```csharp
  Task OnWorkItemStatusChangedAsync(WorkItem item, string oldStatus, string newStatus);
  ```
- [ ] Registration of observer: `FacturationObserver : IWorkItemObserver`
  - Triggers on: `newStatus == "Confirmed" && item.Attributes["IsCorporate"] == true`
- [ ] Workflow:
  1. Retrieve attendee's company info from SQLite
  2. Aggregate all orders for this attendee (status=Confirmed, not yet Invoiced)
  3. Build invoice payload:
     ```json
     {
       "invoiceId": "INV-SF-20260218-001",
       "companyName": "Acme Corp",
       "attendee": "John Doe",
       "items": [
         {"description": "Shiftfestival Registration", "amount": 50.00},
         {"description": "2x Coffee", "amount": 5.00}
       ],
       "total": 55.00,
       "dueDate": "2026-03-18"
     }
     ```
  4. Publish to `shiftfestival.invoices.create` queue
- [ ] Sad Path: RabbitMQ down
  - Mark item with tag `SystemRetry=true`
  - Add to `DispatchBacklog` table for retry
  - Monitoring alert to admins
- [ ] Unit tests with mocked IMessageBus and observer

---

### STORY 3.2: The "Spreker Vertraging" Workflow (Speaker Delay)

**Priority**: 🟡 HIGH  
**Effort**: 5 points  
**Owner**: Dev (Backend)

**Description**:
When a speaker is delayed, auto-update session times and notify attendees.

**Acceptance Criteria**:
- [ ] Workflow trigger: Receive message from Team Planning queue
  ```json
  {
    "type": "session.updated",
    "sessionId": "KEYNOTE-001",
    "delayMinutes": 30,
    "newStartTime": "2026-04-15T11:30:00Z"
  }
  ```
- [ ] Actions:
  1. Update session `StartTime` and `EndTime` in SQLite
  2. Find all attendees registered for this session
  3. Publish to mailing queue:
     ```json
     {
       "to": ["attendee1@example.com", "attendee2@example.com"],
       "subject": "Session Update: Keynote delayed by 30 mins",
       "body": "..."
     }
     ```
  4. Trigger `SessionUpdatedObserver` for any cascading effects
- [ ] Unit tests: Message validation, list building, queue publish

---

### STORY 3.3: Bar/Catering Order Processing (High Performance)

**Priority**: 🔴 CRITICAL  
**Effort**: 6 points  
**Owner**: Dev (Backend) + Infra

**Description**:
Process badge scans at the bar with sub-100ms latency.

**Acceptance Criteria**:
- [ ] Endpoint: `POST /api/bar/order`
  ```json
  Body: {
    "badgeId": "QR-20260218-001",
    "consumableId": "COFFEE-001",
    "quantity": 2
  }
  Response: {
    "orderId": "ORD-12345",
    "totalPrice": 5.00,
    "attendeeName": "John Doe",
    "isCorporate": true,
    "message": "Order added to invoice"
  }
  ```
- [ ] Performance constraints:
  - SlimSemaphore on SQLite write lock (max 1 concurrent write)
  - Query optimization: Badge lookup via indexed BadgeID
  - Transaction: `INSERT INTO orders + UPDATE consumables_inventory` (atomic)
- [ ] Latency target: < 100ms (p95)
  - Implement query caching: Badge → Attendee (2 min TTL)
- [ ] WAL mode ensures readers aren't blocked
- [ ] Concurrency test: 20 concurrent badge scans → All succeed < 100ms

---

### STORY 3.4: Private Attendee Fallback (Sad Path: No Invoice)

**Priority**: 🟡 HIGH  
**Effort**: 3 points  
**Owner**: Dev (Backend)

**Description**:
Private attendees pay immediately; corporates get invoiced. Handle the exception where a private person requests an invoice anyway.

**Acceptance Criteria**:
- [ ] Add attribute to attendee: `RequestsInvoice: boolean` (default false)
- [ ] Workflow Decision:
  ```
  if (isCorporate) → Publish to invoices queue
  else if (!isCorporate && requestsInvoice) → Publish to invoices queue
  else → Mark as "Pay at Bar" in order.Status
  ```
- [ ] Admin interface to toggle `RequestsInvoice` during event
- [ ] Unit tests covering both paths

---

### STORY 3.5: Conflicting Data Resolution

**Priority**: 🟡 HIGH  
**Effort**: 4 points  
**Owner**: Tech Lead / CRM Team

**Description**:
Handle scenarios where external systems have conflicting data (e.g., email changed in CRM but not in registration).

**Acceptance Criteria**:
- [ ] Implement `ConflictResolutionStrategy`:
  - Last-write-wins (default): External system overwrites Masala
  - Masala-primary: Keep Masala version, log conflict
  - Manual: Flag in dashboard for admin review
- [ ] Configuration in YAML:
  ```yaml
  dataConflictPolicy: "last-write-wins"
  ```
- [ ] Log all conflicts to `DataConflicts` table with evidence
- [ ] Admin endpoint: `GET /admin/conflicts` → List all unresolved conflicts
- [ ] Unit tests: Simulate overlapping updates

---

## EPIC 4: Monitoring & Admin Dashboard

### Why This Matters
Course requirement: "Admins ditto verwittigd zodra een systeem down gaat." + Analytics dashboard.

---

### STORY 4.1: System Health Dashboard

**Priority**: 🟡 HIGH  
**Effort**: 8 points  
**Owner**: Frontend Dev + Backend (Elastic integration)

**Description**:
Admin panel showing uptime/downtime of all integrated systems.

**Acceptance Criteria**:
- [ ] Dashboard displays:
  - System name (Drupal, Odoo, FOSSBilling, SendGrid, Masala, RabbitMQ)
  - Status: Green (Healthy), Yellow (Degraded), Red (Down)
  - Last heartbeat timestamp
  - Response time (latency)
  - Message queue depth
- [ ] Data source: Consume heartbeat messages from `monitoring.heartbeats` queue
  - Store in Elasticsearch (if Elastic team ready) or SQLite `SystemHealth` table
- [ ] Alerts:
  - If heartbeat missing for > 5 seconds → Status = Down
  - Send Slack/Email notification to admin
- [ ] Refresh rate: Real-time via WebSocket or polling every 2 seconds
- [ ] Historical view: Charts of uptime over last 24h/7d/30d

---

### STORY 4.2: Event Analytics Dashboard

**Priority**: 🟢 MEDIUM  
**Effort**: 5 points  
**Owner**: Frontend Dev

**Description**:
Social media-ready statistics: registrations, session attendance, revenue.

**Acceptance Criteria**:
- [ ] Displays:
  - Total registrations, breakdown by company vs. private
  - Popular sessions (most registrations)
  - Revenue: Total invoices, collected payments, outstanding
  - Attendance (from badge scans via IoT)
  - Dietary requirements summary
- [ ] Data queries: Use SQLite FTS5 for aggregations
- [ ] Export: CSV download for social media posts
- [ ] Refresh interval: Every 15 minutes

---

### STORY 4.3: Admin Workflow Management

**Priority**: 🟡 HIGH  
**Effort**: 4 points  
**Owner**: Frontend Dev

**Description**:
Manual override for exceptional cases (speaker delay, private-to-corporate upgrade, invoice adjustments).

**Acceptance Criteria**:
- [ ] Pages:
  - Manage Sessions: Update title, time, speaker, capacity
  - Manage Attendees: Search, view profile, toggle IsCorporate, attach company
  - Manage Orders: View pending orders, manually mark as paid
  - Manage Invoices: Edit invoice, send payment reminder
- [ ] Audit log: Track all admin actions with timestamp and user
- [ ] Permission levels: Admin vs. EventManager vs. Viewer

---

## EPIC 5: IoT & Badge Integration

### Why This Matters
Course "extra": Badge scanner for entry + bar payments. High-speed card reader integration.

---

### STORY 5.1: IoT Badge Scan Endpoint

**Priority**: 🟢 MEDIUM  
**Effort**: 4 points  
**Owner**: Dev (Backend) + Infra (Raspberry Pi)

**Description**:
Lightweight endpoint for Raspberry Pi to send QR code scans.

**Acceptance Criteria**:
- [ ] Endpoint: `POST /api/iot/scan` (ultra-lightweight)
  ```json
  Request: {
    "badgeId": "QR-20260218-001",
    "location": "ENTRANCE|BAR|WORKSHOP",
    "timestamp": "2026-02-18T10:00:00Z"
  }
  Response: {
    "status": "ok",
    "message": "Badge scanned"
  }
  ```
- [ ] Actions:
  1. Lookup attendee via BadgeID index
  2. Log scan event to `BadgeScanLog` table (for attendance tracking)
  3. If location=BAR → Trigger order workflow (Story 3.3)
  4. If location=ENTRANCE → Increment attendance counter for that session
- [ ] Performance: < 50ms response time
- [ ] Error handling:
  - Invalid badge → Return 404 (silent, no error to Raspberry Pi)
  - Duplicate scan (same badge within 10 sec) → Deduplicate
- [ ] Rate limiting: 1000 scans/minute per Raspberry Pi

---

### STORY 5.2: Attendance Tracking (Session Occupancy)

**Priority**: 🟢 MEDIUM  
**Effort**: 3 points  
**Owner**: Dev (Backend)

**Description**:
Track how many people actually showed up to each session (vs. registered).

**Acceptance Criteria**:
- [ ] Badge scan at ENTRANCE → Increment `Sessions.AttendanceCount`
- [ ] Dashboard shows: Registered vs. Actual Attendance for each session
- [ ] Export attendance list per session (for speaker/organizer)
- [ ] Unit tests: Badge scan → Attendance increment

---

## EPIC 6: AI & Advanced Integration

### Why This Matters
Course "extra" for bonus points. Allows natural language queries against event data.

---

### STORY 6.1: MCP (Model Context Protocol) Endpoint

**Priority**: 🟢 MEDIUM  
**Effort**: 6 points  
**Owner**: Dev (Backend) + AI Team Lead

**Description**:
Enable external AI agent to query event data without direct SQL access.

**Acceptance Criteria**:
- [ ] Endpoint: `POST /mcp/query` (or WebSocket)
  ```json
  Request: {
    "query": "How many vegetarian attendees are registered for the keynote?",
    "context": "shiftfestival"
  }
  Response: {
    "answer": "12 vegetarian attendees registered for KEYNOTE-001",
    "confidence": 0.95,
    "data": [...],
    "queryTime": "145ms"
  }
  ```
- [ ] Implementation:
  - Use SQLite FTS5 (Full Text Search) on attendee descriptions
  - Pre-compile SELECT queries for common intents:
    - Count by dietary, by session, by company, etc.
  - NO raw SQL injection allowed
- [ ] Query limit: 100 queries/minute per API key
- [ ] Security: API key required, audit logging
- [ ] Example queries the AI should handle:
  - "Total revenue from corporate attendees"
  - "Sessions at capacity"
  - "Attendees from Tech Companies"
  - "Bar revenue vs. registration revenue"

---

### STORY 6.2: Conflict Detection & Alert System

**Priority**: 🟡 HIGH  
**Effort**: 4 points  
**Owner**: Dev (Backend)

**Description**:
Use rules engine to detect and alert on business rule violations (e.g., double-booking, payment conflicts).

**Acceptance Criteria**:
- [ ] Rules defined in YAML:
  ```yaml
  rules:
    - name: "Double Booking"
      trigger: "attendee.selectedSessions.count > capacity"
      action: "alert_admin"
    - name: "Unpaid Invoice"
      trigger: "invoice.daysOverdue > 7"
      action: "send_email_reminder"
    - name: "Budget Overrun"
      trigger: "totalRevenue < targetRevenue * 0.8"
      action: "alert_eventmanager"
  ```
- [ ] Rules evaluated nightly (or on-demand)
- [ ] Violations logged to `RuleViolations` table
- [ ] Admin dashboard shows violations with suggested actions
- [ ] Unit tests for each rule

---

## EPIC 7: External System Integrations (Team Stubs)

### Why This Matters
These are dependent on other teams. We create adapters/receivers for their messages.

---

### STORY 7.1: Drupal Registration Receiver Stub

**Priority**: 🟡 HIGH  
**Effort**: 2 points  
**Owner**: Dev (Backend)

**Description**:
Prepare for receiving registration data from Drupal (other team).

**Acceptance Criteria**:
- [ ] Queue: `shiftfestival.registrations.created`
- [ ] Message schema (JSON):
  ```json
  {
    "id": "REG-UUID",
    "timestamp": "ISO8601",
    "email": "string",
    "firstName": "string",
    "lastName": "string",
    "company": "string (nullable)",
    "dietaryRequirements": "string (nullable)",
    "selectedSessions": ["session-id-1", "session-id-2"]
  }
  ```
- [ ] Receiver validates and transforms to WorkItem
- [ ] Dead letter queue for invalid messages
- [ ] Documentation link: Provide to Drupal team

---

### STORY 7.2: Odoo (Kassa) Order Receiver

**Priority**: 🔴 CRITICAL  
**Effort**: 2 points  
**Owner**: Dev (Backend)

**Description**:
Receive order data from Odoo when attendee pays for consumables at in-venue bar/kassa.

**Acceptance Criteria**:
- [ ] Queue: `shiftfestival.orders.created`
- [ ] Message schema:
  ```json
  {
    "id": "ORDER-UUID",
    "timestamp": "ISO8601",
    "badgeId": "QR-string",
    "items": [
      {"consumableId": "COFFEE-001", "quantity": 2, "unitPrice": 2.50}
    ],
    "totalPrice": 5.00,
    "paymentStatus": "Paid|Pending"
  }
  ```
- [ ] Receiver creates order WorkItem
- [ ] Triggers FacturationObserver if corporate

---

### STORY 7.3: FOSSBilling Sender (Invoice Dispatch)

**Priority**: 🟡 HIGH  
**Effort**: 2 points  
**Owner**: Dev (Backend) / Facturatie Team

**Description**:
Stub for sending invoices to FOSSBilling.

**Acceptance Criteria**:
- [ ] Queue: `shiftfestival.invoices.create`
- [ ] Message structure (to be detailed with Facturatie team):
  ```json
  {
    "invoiceId": "INV-string",
    "clientEmail": "email",
    "items": [...],
    "amount": 0.00,
    "dueDate": "ISO8601"
  }
  ```
- [ ] Placeholder receiver on FOSSBilling side (for testing)

---

### STORY 7.4: SendGrid Sender (Email Dispatch)

**Priority**: 🟡 HIGH  
**Effort**: 3 points  
**Owner**: Dev (Backend) / Mailing Team

**Description**:
Send emails for confirmations, delays, reminders.

**Acceptance Criteria**:
- [ ] Queue: `shiftfestival.emails.send`
- [ ] Message schema:
  ```json
  {
    "recipients": ["email1", "email2"],
    "subject": "string",
    "templateId": "registration_confirmation|session_delay|payment_reminder",
    "variables": {
      "attendeeName": "string",
      "sessionTitle": "string",
      ...
    }
  }
  ```
- [ ] SendGrid integration: Use official SDK
- [ ] Retry logic for failed sends
- [ ] Bounce handling: Move recipients to DLC (Do Not Contact)

---

### STORY 7.5: Planning (Office365) Integration Receiver

**Priority**: 🟡 HIGH  
**Effort**: 3 points  
**Owner**: Dev (Backend) / Planning Team

**Description**:
Listen for session schedule updates from Office365 calendar.

**Acceptance Criteria**:
- [ ] Queue: `shiftfestival.sessions.updated`
- [ ] Message schema: Speaker delay, room change, cancellation
- [ ] Trigger cascading updates (Story 3.2)

---

### STORY 7.6: Salesforce CRM Sender (Contact Upsert)

**Priority**: 🟢 MEDIUM  
**Effort**: 4 points  
**Owner**: Dev (Backend) / CRM Team

**Description**:
Sync attendee & company data to Salesforce after event (for future business).

**Acceptance Criteria**:
- [ ] Queue: `shiftfestival.crm.upsert`
- [ ] Message schema:
  ```json
  {
    "type": "Contact|Account",
    "externalId": "email or company-id",
    "data": {
      "FirstName": "string",
      "LastName": "string",
      "Email": "string",
      "Company": "string",
      "Phone": "string (nullable)",
      "AttendedSessions": ["session-id-1"],
      "Notes": "string"
    }
  }
  ```
- [ ] Batching: Send nightly after event (bulk API)
- [ ] Idempotency: externalId ensures no duplicates

---

## EPIC 8: DevOps & CI/CD Pipeline

### Why This Matters
Course requirement: "Fully automated pipeline for deployment."

---

### STORY 8.1: Docker Image Build (Prod-Ready)

**Priority**: 🟡 HIGH  
**Effort**: 3 points  
**Owner**: Infra Team

**Description**:
Build & push Ticket Masala Docker image to registry.

**Acceptance Criteria**:
- [ ] Dockerfile:
  - Multi-stage build (build stage + runtime stage)
  - .NET 8 base image
  - SQLite included in image
  - Non-root user
- [ ] GitHub Actions workflow: `docker-build.yml`
  - Trigger on: push to main, dev, prod branches
  - Build → Test → Push to registry
  - Tag: `garamatic/ticket-masala:${git-sha}` and `:latest` (if main)

---

### STORY 8.2: GitHub Branches & Protections

**Priority**: 🟡 HIGH  
**Effort**: 2 points  
**Owner**: Tech Lead

**Description**:
Set up branching strategy: main (prod) → prod (releases) → dev (staging) → feature branches.

**Acceptance Criteria**:
- [ ] Branches:
  - `main`: Production-ready. Protected. Merges from `prod` only.
  - `prod`: Pre-release. Merges from `dev` after testing.
  - `dev`: Development. Merges from feature branches.
  - `feature/*`: Individual feature work.
- [ ] Branch protections:
  - `main` & `prod`: Require 1+ code review
  - `main`: All CI checks must pass
  - No force pushes allowed
- [ ] Merge strategy: Squash & merge (clean history)

---

### STORY 8.3: Automated Testing Pipeline (CI)

**Priority**: 🟡 HIGH  
**Effort**: 5 points  
**Owner**: Dev (Backend) + QA

**Description**:
Run unit tests, integration tests, and contract tests on every push.

**Acceptance Criteria**:
- [ ] GitHub Actions workflow: `test.yml`
  - Trigger on: push to any branch
  - Steps:
    1. Checkout code
    2. Setup .NET SDK
    3. Restore dependencies
    4. Run unit tests (xUnit)
    5. Run integration tests (testcontainers: SQLite, RabbitMQ)
    6. Coverage report (Coverlet) → Codecov
    7. Message contract tests (Story 1.2 & 1.3)
- [ ] Minimum coverage: 70% for `src/` (fail if < 70%)
- [ ] Test timeout: 5 minutes
- [ ] Failure: Blocks PR merge

---

### STORY 8.4: Deployment to Fly.io (Dev/Staging/Prod)

**Priority**: 🟡 HIGH  
**Effort**: 6 points  
**Owner**: Infra Team

**Description**:
Automated deployment via `fly deploy`.

**Acceptance Criteria**:
- [ ] Fly config files (already exist):
  - `fly.toml` (app name, region, etc.)
  - Environment secrets in Fly dashboard
- [ ] GitHub Actions workflow: `deploy.yml`
  - Trigger: On push to `main` (prod) or `dev` (staging)
  - Build Docker image (Story 8.1)
  - Deploy to Fly.io via `flyctl deploy`
  - Post-deploy health check: Ping `/health` endpoint
  - On failure: Rollback previous version, alert ops
- [ ] Health endpoint: `GET /health`
  - Returns `{"status": "healthy"}` if all checks pass
  - Checks: DB connectivity, RabbitMQ connectivity, disk space

---

### STORY 8.5: Secrets Management

**Priority**: 🟡 HIGH  
**Effort**: 2 points  
**Owner**: Infra Team

**Description**:
Store sensitive configs (RabbitMQ, DB, API keys) securely.

**Acceptance Criteria**:
- [ ] GitHub Actions: Use GitHub Secrets for sensitive values
  - `RABBITMQ_URI`
  - `SENDGRID_API_KEY`
  - `SALESFORCE_CLIENT_ID` & `CLIENT_SECRET`
- [ ] Fly.io: Inject via `fly secrets set` CLI
- [ ] Local dev: `.env` file (git-ignored)
- [ ] audit: List all secrets used in workflows
- [ ] Rotation: Document how to rotate keys

---

## EPIC 9: Testing, Load Testing, Stress Testing

### Why This Matters
Prepare for production load (hundreds of concurrent badge scans, spike in registrations during event).

---

### STORY 9.1: Unit Tests (Minimum 70% Coverage)

**Priority**: 🟡 HIGH  
**Effort**: 8 points (ongoing)  
**Owner**: Dev (Backend)

**Description**:
Comprehensive unit tests for all services.

**Acceptance Criteria**:
- [ ] Test categories:
  - Message transformation (Drupal XML → WorkItem)
  - Observer triggers and actions
  - Conflict resolution logic
  - YAML configuration loading
  - SQLite schema generation
- [ ] Naming: `ClassName_MethodName_ExpectedBehavior.cs`
- [ ] Mocking: Moq for IMessageBus, IWorkItemRepository, etc.
- [ ] Arrange-Act-Assert pattern
- [ ] Test data: Use builders (e.g., `WorkItemBuilder.cs`)

---

### STORY 9.2: Integration Tests (End-to-End Message Flow)

**Priority**: 🟡 HIGH  
**Effort**: 10 points (ongoing)  
**Owner**: Dev (Backend) + QA

**Description**:
Test complete workflows: registration → invoice → email.

**Acceptance Criteria**:
- [ ] Setup:
  - Testcontainers for SQLite (fresh DB each test)
  - Testcontainers for RabbitMQ (fresh instance)
- [ ] Test scenarios:
  1. Registration creates attendee + publishes email
  2. Bar order for corporate → Invoice generated → Email sent
  3. Speaker delay → Session updated → Attendees emailed
  4. Private attendee requests invoice → Exception handled
- [ ] Assertions:
  - DB state (attendee exists, order created)
  - Queue messages (verified in RabbitMQ)
  - Emails queued for SendGrid
- [ ] Timeout: 30 seconds per test (containers take time to start)

---

### STORY 9.3: Load Test (Concurrent Badge Scans)

**Priority**: 🟢 MEDIUM  
**Effort**: 5 points  
**Owner**: Infra / QA

**Description**:
Simulate 50+ concurrent badge scans at the bar during peak time.

**Acceptance Criteria**:
- [ ] Tool: k6 (lightweight load testing)
- [ ] Scenario:
  ```javascript
  // Pseudo k6 script
  import http from 'k6/http';
  export default () => {
    http.post('http://localhost:5000/api/bar/order', {
      badgeId: `QR-${__VU}-${__ITER}`,
      consumableId: 'COFFEE-001',
      quantity: 1
    });
  };
  export const options = {
    vus: 50,        // 50 virtual users
    duration: '60s', // 60 seconds
  };
  ```
- [ ] Success criteria:
  - 95th percentile response time < 150ms
  - Error rate < 1%
  - No database deadlocks
- [ ] Report: Summary of throughput, latencies, errors

---

### STORY 9.4: Message Contract Testing

**Priority**: 🟡 HIGH  
**Effort**: 4 points  
**Owner**: Dev (Architect Lead)

**Description**:
Verify message schemas match expectations (Pact-style testing).

**Acceptance Criteria**:
- [ ] Tool: Azure Service Bus integration tests or pact-net
- [ ] Test every queue:
  - `shiftfestival.registrations.created` ← Drupal
  - `shiftfestival.orders.created` ← Odoo
  - `shiftfestival.invoices.create` → FOSSBilling
  - `shiftfestival.emails.send` → SendGrid
- [ ] Assertions:
  - Required fields present
  - Data types correct
  - Timestamps valid ISO8601
- [ ] Contract docs auto-generated for other teams

---

## EPIC 10: Documentation & Knowledge Transfer

### Why This Matters
Course requirement: Teams must understand the overall system. Clear docs reduce friction.

---

### STORY 10.1: Architecture Decision Records (ADRs)

**Priority**: 🟡 HIGH  
**Effort**: 4 points  
**Owner**: Tech Lead

**Description**:
Document major architectural decisions.

**Acceptance Criteria**:
- [ ] ADRs created:
  1. Why RabbitMQ (async, loose coupling, retries)
  2. Why SQLite WAL mode (concurrent bar scans)
  3. Why YAML configuration (tenancy, no recompile)
  4. Why observer pattern (extensible workflows)
- [ ] Format: Status, Context, Decision, Consequences, Alternatives
- [ ] Location: `docs/adr/` folder
- [ ] Link from README

---

### STORY 10.2: API Documentation (OpenAPI/Swagger)

**Priority**: 🟡 HIGH  
**Effort**: 3 points  
**Owner**: Dev (Backend)

**Description**:
Auto-generate API docs from C# code.

**Acceptance Criteria**:
- [ ] Tool: Swashbuckle (NSwag) for .NET
- [ ] Endpoints documented:
  - `POST /api/registrations`
  - `POST /api/bar/order`
  - `GET /admin/health`
  - `GET /admin/dashboard`
  - `POST /mcp/query`
  - `POST /api/iot/scan`
- [ ] Swagger UI available at `/swagger/index.html`
- [ ] Each endpoint has:
  - Description
  - Request/response schemas
  - Example values
  - Error codes

---

### STORY 10.3: Integration Guide for External Teams

**Priority**: 🟡 HIGH  
**Effort**: 5 points  
**Owner**: Tech Lead

**Description**:
Guide explaining message formats, queues, and retry logic for Drupal, Odoo, SendGrid teams.

**Acceptance Criteria**:
- [ ] Document: `docs/INTEGRATION_GUIDE.md`
- [ ] Sections:
  1. Message Format (JSON schema for each queue)
  2. Queue Names & Endpoints (where to connect)
  3. Error Handling (retry logic, DLC)
  4. Testing (how to use contract tests)
  5. Code Examples (C#, Python, Node.js snippets for message publishing)
- [ ] Example: "How to send a registration message from Drupal"
- [ ] Link provided to each team lead

---

### STORY 10.4: Runbook for Operations

**Priority**: 🟡 HIGH  
**Effort**: 3 points  
**Owner**: Infra Team

**Description**:
Operational procedures for admins during the event.

**Acceptance Criteria**:
- [ ] Runbook: `docs/RUNBOOK.md`
- [ ] Sections:
  1. Pre-event (startup checks, DB backup)
  2. During event (monitor dashboard, handle alerts)
  3. Post-event (data export, cleanup, archive)
- [ ] Common issues:
  - RabbitMQ queue backing up → Increase prefetch
  - Badge scanner not responding → Restart on Raspberry Pi
  - Duplicate invoices → Run deduplication script
  - Database locked → Kill long-running query
- [ ] Escalation: When to contact each team

---

## Summary: Task Priority & Sequencing

### Phase 1: Foundation (Weeks 1-2)
Critical path for everything else:
1. Story 1.1: RabbitMQ setup
2. Story 2.1-2.3: YAML config + SQLite schema
3. Story 8.2: Git branches
4. Story 8.3: CI pipeline

### Phase 2: Core Workflows (Weeks 3-4)
1. Story 1.2-1.3: Message ingress/egress
2. Story 1.4: Heartbeat
3. Story 3.1-3.4: Observer patterns & workflows
4. Story 3.3: Bar ordering (performance critical)

### Phase 3: External Integration (Weeks 5-6)
1. Story 7.1-7.6: Receiver/sender stubs
2. Story 4.1-4.3: Dashboards & admin UI
3. Story 9.1-9.4: Testing & load tests

### Phase 4: Extras & Polish (Weeks 7-8)
1. Story 5.1-5.2: IoT badge (nice to have)
2. Story 6.1-6.2: AI/MCP, conflict detection
3. Story 10.1-10.4: Documentation
4. Story 8.4-8.5: Full CI/CD & secrets

---

## Definitions & Glossary

- **WorkItem**: Internal representation of any entity (Attendee, Order, Session, Invoice).
- **Queue**: RabbitMQ message queue (e.g., `shiftfestival.registrations.created`).
- **Observer**: Design pattern that triggers actions when a WorkItem changes status.
- **Idempotency**: Receiving the same message twice doesn't create duplicates (via MessageId tracking).
- **Sad Path**: Exception scenario (RabbitMQ down, conflicting data, speaker delay).
- **Dead Letter Queue (DLC)**: Repository for messages that failed processing.
- **WAL Mode**: SQLite Write-Ahead Logging; enables concurrent reads while writes happen.
- **Heartbeat**: Periodic "I am alive" message every second.
- **MCP**: Model Context Protocol; allows external AI to query data safely.
- **Pact**: Contract testing tool for verifying message schemas.
- **FTS5**: Full Text Search in SQLite; enables natural language queries.

---

## Key Success Metrics

By end of project:
- ✅ All 10 epics addressed (even if some stories partial)
- ✅ % test coverage ≥ 70%
- ✅ Bar scan latency p95 < 150ms under 50 concurrent VUs
- ✅ Zero system downtime during 4-hour live event
- ✅ All teams can independently develop against message contracts
- ✅ Admins can manage event via dashboard without CLI access
- ✅ Data exported to Salesforce post-event for follow-up

---

## Questions for Project Manager / Customer (School)

1. **Event Date & Scale**: When is Shiftfestival? How many attendees expected (100? 500? 1000)?
2. **Bar Hours**: How long is the bar open? Peak concurrent badge scans?
3. **Email Template**: Who designs email templates (team mailing or marketing)?
4. **Invoice Format**: Does Desiderius have preferred invoice format (standard or custom)?
5. **Dietary Data**: Is dietary info mandatory? How detailed (vegetarian/vegan, allergies)?
6. **Salesforce Account**: Is a trial account pre-provisioned for the CRM team?
7. **Raspberry Pi Setup**: Who provides & configures the badge scanners?
8. **Language**: Dutch or English for user-facing messages?
