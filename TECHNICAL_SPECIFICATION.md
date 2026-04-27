# Technical Specification: Masala Bridge Pattern

**Version**: 1.0  
**Date**: February 18, 2026  
**Status**: Approved for Implementation

---

## 1. System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     EXTERNAL SYSTEMS                             │
│   Drupal        Odoo          FOSSBilling   SendGrid   Azure...  │
└──────────────────────────────────────────────────────────────────┘
                            ↕ (Decoupled via)
┌─────────────────────────────────────────────────────────────────┐
│                      RabbitMQ Message Bus                        │
│  Queues: *.registrations.created, *.orders.created, etc.        │
└──────────────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────────────┐
│               Masala Bridge Layer (THIS PROJECT)                │
│                                                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ RabbitMQ Ingress Service  │  RabbitMQ Dispatcher Service │  │
│  │ (Receivers/Transformers)  │  (Senders/Publishers)        │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            ↓                                      │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │       System.Threading.Channels (In-Memory Queue)        │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            ↓                                      │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │      GERDA Workflow Engine (Domain Logic)                │  │
│  │  • Observers (Facturatie, Mailing, CRM)                 │  │
│  │  • Rule Engine (Conflict detection, Automation)         │  │
│  │  • Event sourcing (Immutable log)                        │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            ↓                                      │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │     SQLite 3 (WAL Mode) + FTS5 (Full-Text Search)       │  │
│  │  • Attendees, Sessions, Orders, Invoices                │  │
│  │  • BadgeScanLog, DataConflicts, SystemHealth            │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                   │
│  Admin Interfaces:                                               │
│  • REST API (CRUD operations)                                   │
│  • MCP Endpoint (AI queries)                                    │
│  • Dashboard (Blazor/Vue.js)                                    │
│  • IoT Endpoint (Badge scanner)                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Key Principle: **Loose Coupling**
- Masala doesn't know internal details of Drupal, Odoo, etc.
- All communication via message contracts (JSON schema).
- If Odoo KPIs offline → Masala continues (graceful degradation).
- Admin notified → Manual intervention possible.

---

## 2. Message Bus Architecture (RabbitMQ)

### Queue Naming Convention
```
shiftfestival.<entity>.<action>.<direction>

Examples:
  shiftfestival.registrations.created       ← from Drupal
  shiftfestival.registrations.confirmed     → internal
  shiftfestival.orders.created              ← from Odoo
  shiftfestival.invoices.create             → to FOSSBilling
  shiftfestival.emails.send                 → to SendGrid
  shiftfestival.sessions.updated            ← from Planning (Office365)
  shiftfestival.crm.upsert                  → to Salesforce
  monitoring.heartbeats                     ← from all systems
  monitoring.alerts                         → to Admins
```

### Connection & Consumer Setup

**Connection String** (appsettings.json):
```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ConnectionName": "TicketMasala.Consumer",
    "AutomaticRecoveryEnabled": true,
    "NetworkRecoveryInterval": 10000,
    "HeartbeatInterval": 60,
    "RequestedChannelMax": 512
  }
}
```

**Consumer Configuration**:
```csharp
services.AddSingleton<IConnectionFactory>(sp => 
{
    var factory = new ConnectionFactory
    {
        HostName = config["RabbitMQ:Host"],
        UserName = config["RabbitMQ:Username"],
        Password = config["RabbitMQ:Password"],
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
    };
    return factory;
});

// IHostedService registers consumers
services.AddHostedService<RabbitMqIngressService>();
services.AddHostedService<HeartbeatPublisher>();
```

### Poison Pill & Dead Letter Queues

Every consumer subscribes to TWO queues:
1. **Main queue**: `shiftfestival.registrations.created`
2. **DLQ (dead letter queue)**: `shiftfestival.registrations.created.dlq`

If message processing fails 3+ times → Move to DLQ.

**DLQ Processing** (daily batch job):
- Admin reviews failed message
- Corrects payload if needed → Republish
- Or discard if unrecoverable

```csharp
[Queue("shiftfestival.registrations.created.dlq")]
public async Task ProcessDeadLetter(RegistrationMessage msg)
{
    logger.LogError("DLQ: Unable to process registration {Id}", msg.Id);
    await db.SaveAsync(new DeadLetterEvent 
    { 
        QueueName = "registrations",
        Payload = JsonConvert.SerializeObject(msg),
        FailureReason = msg.LastError,
        CreatedAt = DateTime.UtcNow 
    });
}
```

---

## 3. Data Pipeline: External → Masala → External

### 3.1 Inbound Messages (Receivers)

#### Example: Registration from Drupal

```
┌─────────────────────────────────────────────────────────────────┐
│ DRUPAL                                                            │
│ POST /webhook/masala/registration                                │
│ Body: Drupal's internal format (probably XML or JSON)           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ RabbitMQ                                                          │
│ Queue: shiftfestival.registrations.created                      │
│ Payload: Standardized JSON (contract)                           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ MASALA INGRESS (RabbitMqIngressService)                         │
│                                                                   │
│ 1. Consume message from `shiftfestival.registrations.created`   │
│ 2. Deserialize JSON → RegistrationMessage (DTO)                │
│ 3. Validate against schema (XSD or JSON Schema)                │
│ 4. Check idempotency: Is MessageId in IngestedMessages table?  │
│ 5. Transform to WorkItem (domain model):                       │
│    RegistrationMessage → WorkItem {                            │
│      EntityType = "Registration",                              │
│      EntityId = registration.id,                               │
│      Attributes = { Email, Name, Company, Sessions, ... },    │
│      Status = "Pending",                                       │
│      CreatedAt = now                                           │
│    }                                                             │
│ 6. Store WorkItem in SQLite (atomic transaction)               │
│ 7. Enqueue WorkItem to internal System.Threading.Channels      │
│ 8. Ack message to RabbitMQ                                     │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ GERDA WORKFLOW ENGINE (Process WorkItem)                        │
│                                                                   │
│ 1. Observers subscribed to "Registration" WorkItems listen     │
│ 2. Example: NotificationObserver                               │
│    - Detects: WorkItem.Status == "Pending"                     │
│    - Action: Send confirmation email                           │
│ 3. Example: CrmObserver                                        │
│    - Detects: Attribute["Company"] is not null                 │
│    - Action: Queue CRM upsert message                          │
└─────────────────────────────────────────────────────────────────┘
```

**Message Contract** (Drupal → Masala):
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "title": "Registration Created",
  "properties": {
    "id": { "type": "string", "description": "Unique registration UUID" },
    "timestamp": { "type": "string", "format": "date-time" },
    "email": { "type": "string", "format": "email" },
    "firstName": { "type": "string" },
    "lastName": { "type": "string" },
    "company": { "type": ["string", "null"] },
    "dietaryRequirements": { "type": ["string", "null"], "enum": ["vegetarian", "vegan", "glutenfree", null] },
    "selectedSessions": { "type": "array", "items": { "type": "string" }, "minItems": 1 },
    "paymentMethod": { "type": "string", "enum": ["invoice", "card"] }
  },
  "required": ["id", "timestamp", "email", "firstName", "lastName", "selectedSessions"]
}
```

### 3.2 Outbound Messages (Senders)

#### Example: Invoice to FOSSBilling

```
┌─────────────────────────────────────────────────────────────────┐
│ GERDA WORKFLOW ENGINE (Observer)                               │
│ FacturationObserver triggered by: Status == "Confirmed"        │
│                                                                   │
│ 1. Load attendee + all confirmed orders                        │
│ 2. Calculate total (registration fee + consumables)            │
│ 3. Create invoice WorkItem                                     │
│ 4. Status transition: Pending → InvoiceRequested               │
│ 5. Call IMessageBus.PublishAsync(...)                          │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ MASALA DISPATCHER (RabbitMqDispatcherService)                   │
│                                                                   │
│ 1. Serialize invoice payload to JSON                           │
│ 2. Create MessageEnvelope:                                     │
│    {                                                             │
│      "messageId": "uuid",                                       │
│      "timestamp": "ISO8601",                                    │
│      "source": "TicketMasala",                                  │
│      "type": "InvoiceCreated",                                  │
│      "payload": { ... }                                         │
│    }                                                             │
│ 3. Publish to queue: shiftfestival.invoices.create             │
│ 4. Store in outbox table: DispatchedMessages                   │
│    (for audit & retry)                                          │
│ 5. If RabbitMQ unavailable: Catch exception                    │
│    - Add to DispatchBacklog queue                              │
│    - Retry every 30 seconds (up to 24 hours)                   │
│    - Alert admin via monitoring.alerts queue                   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ RabbitMQ                                                          │
│ Queue: shiftfestival.invoices.create                           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ FOSSBilling Team (Consumer)                                      │
│ Receives invoice → Creates in their system → Sends email       │
└─────────────────────────────────────────────────────────────────┘
```

**Message Contract** (Masala → FOSSBilling):
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "title": "Create Invoice",
  "properties": {
    "messageId": { "type": "string", "format": "uuid" },
    "timestamp": { "type": "string", "format": "date-time" },
    "invoiceId": { "type": "string", "description": "Unique invoice number" },
    "company": { "type": "string" },
    "attendee": { "type": "object",
      "properties": {
        "name": { "type": "string" },
        "email": { "type": "string", "format": "email" }
      }
    },
    "items": { "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "description": { "type": "string" },
          "quantity": { "type": "integer", "minimum": 1 },
          "unitPrice": { "type": "number", "minimum": 0 },
          "amount": { "type": "number" }
        }
      }
    },
    "subtotal": { "type": "number" },
    "tax": { "type": "number" },
    "total": { "type": "number" },
    "dueDate": { "type": "string", "format": "date" },
    "notes": { "type": "string" }
  },
  "required": ["messageId", "timestamp", "invoiceId", "company", "attendee", "items", "total"]
}
```

---

## 4. Workflow Engine: GERDA Observers

### 4.1 Observer Pattern Flow

```csharp
// 1. Define event
public record WorkItemStatusChangedEvent(
    WorkItem WorkItem,
    string OldStatus,
    string NewStatus,
    DateTime OccurredAt
);

// 2. Observer interface
public interface IWorkItemObserver
{
    Task OnWorkItemStatusChangedAsync(
        WorkItem item,
        string oldStatus,
        string newStatus,
        CancellationToken ct);
}

// 3. Concrete observer
public class FacturationObserver : IWorkItemObserver
{
    public async Task OnWorkItemStatusChangedAsync(
        WorkItem item, string oldStatus, string newStatus, CancellationToken ct)
    {
        // Only trigger for orders that are confirmed AND corporate
        if (item.EntityType != "Order") return;
        if (newStatus != "Confirmed") return;
        if (!item.Attributes.GetBool("IsCorporate")) return;

        // Fetch attendee data
        var attendee = await _db.Attendees.FirstAsync(
            a => a.Id == item.Attributes["AttendeeRef"]);
        
        if (attendee.Company == null) return;

        // Build invoice
        var invoice = new InvoiceWorkItem(attendee, item);

        // Publish to RabbitMQ
        await _messageBus.PublishAsync(
            "shiftfestival.invoices.create",
            invoice,
            ct);

        // Transition WorkItem
        item.Status = "InvoiceRequested";
        await _db.SaveChangesAsync(ct);
    }
}

// 4. Register observer
services.AddScoped<IWorkItemObserver, FacturationObserver>();
services.AddScoped<IWorkItemObserver, NotificationObserver>();
services.AddScoped<IWorkItemObserver, CrmSyncObserver>();

// 5. Trigger from GERDA engine
public class WorkItemService
{
    public async Task TransitionStatusAsync(
        WorkItem item, string newStatus, CancellationToken ct)
    {
        var oldStatus = item.Status;
        item.Status = newStatus;
        
        await _db.SaveChangesAsync(ct); // Persist first

        // Notify all observers
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnWorkItemStatusChangedAsync(
                    item, oldStatus, newStatus, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer failed: {ObserverType}", 
                    observer.GetType().Name);
                // Continue (don't fail entire workflow)
            }
        }
    }
}
```

### 4.2 Example Workflows

#### Workflow 1: Corporate Registration → Auto-Invoice

```
External: Drupal sends registration
           │
           ↓
Masala:  Parse registration → Create WorkItem(status=Pending)
           │
           ↓
Observer: "Is IsCorporate?" YES
          "Is SessionSelected?" YES
           │
           ↓
Action:   Create invoice WorkItem → Publish to FOSSBilling queue
          Send confirmation email to attendee + company contact
           │
           ↓
External: FOSSBilling receives invoice, sends to accounting
          Email team sends confirmation to attendee
```

#### Workflow 2: Speaker Delay → Cascade Updates

```
External: Planning (Office365) sends "Keynote delayed 30 min"
           │
           ↓
Masala:  Publish to shiftfestival.sessions.updated
           │
           ↓
Observer: SessionUpdatedObserver triggers
           │
           ├─ Update session times in SQLite
           ├─ Find all attendees for this session
           └─ Publish to mailing queue
                │
                ↓
External: SendGrid sends emails to all attendees
          Dashboard updates live
```

#### Workflow 3: Bar Scan (High Speed, Low Latency)

```
IoT:     Raspberry Pi scans badge: QR-12345
         POST /api/iot/scan { badgeId, location: "BAR" }
         │
         ↓
Masala:  1. Index lookup: BadgeID → AttendeeID (cached, <1ms)
         2. If IsCorporate:
            - Increment order counter
            - Return: "Charged to invoice"
         3. If Private:
            - Create order in SQLite (WAL Mode)
            - Return: "Amount: €5.00"
         │
         ↓
Response: { status: "ok", totalPrice: 5.00, ... } <50ms
         │
         ↓
Observer: FacturationObserver checks nightly for unpaid invoices
          CrmObserver syncs to Salesforce (post-event)
```

---

## 5. Data Model: YAML-Driven Tenancy

### 5.1 Tenant Configuration Structure

**File**: `config/tenants/shiftfestival/domains.yaml`

```yaml
tenant: shiftfestival
eventName: "Shiftfestival 2026"
eventDate: "2026-04-15T10:00:00Z"
location: "Desiderius Hogeschool"

# Entity types + attributes
domains:
  - name: attendees
    displayName: "Registered Participants"
    pluralName: "attendees"
    description: "People registered for the event"
    attributes:
      - name: Email
        dataType: string
        required: true
        unique: true
        
      - name: FullName
        dataType: string
        required: true
        
      - name: Company
        dataType: string
        required: false
        
      - name: IsCorporate
        dataType: boolean
        default: false
        
      - name: BadgeID
        dataType: string
        required: true
        unique: true
        indexed: true  # Fast lookup for badge scans
        
      - name: DietaryReq
        dataType: enum
        values: [vegetarian, vegan, glutenfree, halal, kosher, none]
        
      - name: SelectedSessions
        dataType: array
        itemDataType: ref
        refDomain: sessions

  - name: sessions
    displayName: "Workshops & Keynotes"
    attributes:
      - name: Title
        dataType: string
        required: true
        
      - name: Speaker
        dataType: string
        
      - name: StartTime
        dataType: datetime
        required: true
        
      - name: EndTime
        dataType: datetime
        required: true
        
      - name: Room
        dataType: string
        
      - name: Capacity
        dataType: integer
        default: 100
        
      - name: Category
        dataType: enum
        values: [KEYNOTE, WORKSHOP, NETWORKING, PANEL, RECEPTION]
        
      - name: AttendanceCount
        dataType: integer
        default: 0
        
  - name: orders
    displayName: "Orders (Bar & Consumables)"
    attributes:
      - name: AttendeeRef
        dataType: ref
        refDomain: attendees
        required: true
        
      - name: ConsumableRef
        dataType: ref
        refDomain: consumables
        required: true
        
      - name: Quantity
        dataType: integer
        default: 1
        
      - name: Status
        dataType: enum
        values: [Pending, Confirmed, Invoiced, Paid]
        default: Pending
        
      - name: CreatedAt
        dataType: datetime
        default: "NOW()"
        
  - name: consumables
    displayName: "Bar Items & Beverages"
    attributes:
      - name: Name
        dataType: string
        required: true
        
      - name: Description
        dataType: string
        
      - name: Price
        dataType: decimal
        precision: 10
        scale: 2
        
      - name: Category
        dataType: enum
        values: [BEVERAGE, FOOD, DESSERT, SPECIAL]
        
      - name: AvailableFrom
        dataType: datetime
        
      - name: AvailableUntil
        dataType: datetime
```

### 5.2 Dynamic Table Generation

**Service**: `SchemaGeneratorService`

```csharp
public class SchemaGeneratorService
{
    public async Task EnsureSchemasAsync(ITenantConfig config, CancellationToken ct)
    {
        foreach (var domain in config.Domains)
        {
            await CreateTableIfNotExistsAsync(domain);
            await CreateIndexesAsync(domain);
            await TrackMigrationAsync(domain);
        }
    }

    private async Task CreateTableIfNotExistsAsync(DomainConfig domain)
    {
        var columns = domain.Attributes
            .Select(attr => $"{attr.Name} {MapToSqliteType(attr)}")
            .ToList();
        
        columns.Insert(0, "id TEXT PRIMARY KEY");
        columns.Add("CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP");
        columns.Add("UpdatedAt DATETIME");

        var sql = $"""
            CREATE TABLE IF NOT EXISTS {domain.Name} (
                {string.Join(", ", columns)}
            )
            """;

        await _db.ExecuteAsync(sql);
    }

    private async Task CreateIndexesAsync(DomainConfig domain)
    {
        var indexedAttrs = domain.Attributes
            .Where(a => a.Indexed || a.Unique)
            .ToList();

        foreach (var attr in indexedAttrs)
        {
            var unique = attr.Unique ? "UNIQUE " : "";
            var sql = $"CREATE {unique}INDEX IF NOT EXISTS idx_{domain.Name}_{attr.Name} " +
                      $"ON {domain.Name}({attr.Name})";
            await _db.ExecuteAsync(sql);
        }
    }

    private string MapToSqliteType(AttributeConfig attr) => attr.DataType switch
    {
        "string" => "TEXT",
        "integer" => "INTEGER",
        "decimal" => $"REAL",  // SQLite doesn't have DECIMAL; use REAL + CHECK
        "boolean" => "INTEGER",  // SQLite: 0 = false, 1 = true
        "datetime" => "DATETIME",
        "enum" => "TEXT",
        "array" => "JSON",
        "ref" => "TEXT",  // FK reference
        _ => throw new InvalidOperationException($"Unknown type: {attr.DataType}")
    };
}
```

---

## 6. SQLite Configuration (Performance & Concurrency)

### 6.1 WAL Mode Activation

```csharp
// Program.cs
services.AddScoped<DbContextFactory>(sp =>
{
    var connectionString = config.GetConnectionString("Sqlite");
    
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    
    // Enable WAL mode
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "PRAGMA journal_mode=WAL;";
    var mode = cmd.ExecuteScalar();
    _logger.LogInformation("SQLite journal_mode: {Mode}", mode);
    
    // Configure WAL
    cmd.CommandText = """
        PRAGMA wal_autocheckpoint = 1000;
        PRAGMA synchronous = NORMAL;
        PRAGMA cache_size = -64000;
        PRAGMA temp_store = MEMORY;
        """;
    cmd.ExecuteNonQuery();
    
    connection.Close();
    
    return /* DbContextFactory */;
});
```

### 6.2 High-Concurrency Operations (Bar Scans)

```csharp
public class BarOrderService
{
    private readonly SemaphoreSlim _dbLock = new(1);  // SlimSemaphore

    public async Task<OrderResult> ProcessBadgeScanAsync(
        string badgeId, string consumableId, int qty, CancellationToken ct)
    {
        // Fast path: Lookup attendee from cache
        var attendee = await _attendeeCache.GetByBadgeIdAsync(badgeId);
        if (attendee == null)
            throw new BadgeNotFoundException();

        // Serialize writes (one at a time)
        await _dbLock.WaitAsync(ct);
        try
        {
            using var transaction = await _db.BeginTransactionAsync(ct);
            
            // Atomic: INSERT + UPDATE in same transaction
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                AttendeeId = attendee.Id,
                ConsumableId = consumableId,
                Quantity = qty,
                CreatedAt = DateTime.UtcNow
            };
            
            await _db.Orders.AddAsync(order, ct);
            
            // Increment attendee's order counter
            attendee.OrderCount++;
            _db.Attendees.Update(attendee);
            
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            
            return new OrderResult { OrderId = order.Id, TotalPrice = /* calc */ };
        }
        finally
        {
            _dbLock.Release();
        }
    }
}
```

### 6.3 FTS5 (Full-Text Search) for MCP Queries

```csharp
// Enable FTS5
using var cmd = connection.CreateCommand();
cmd.CommandText = """
    CREATE VIRTUAL TABLE IF NOT EXISTS attendees_fts USING fts5(
        id UNINDEXED,
        email,
        fullname,
        company,
        dietaryreq,
        content = attendees,
        content_rowid = id
    );
    """;
cmd.ExecuteNonQuery();

// Trigger to keep FTS5 in sync
cmd.CommandText = """
    CREATE TRIGGER IF NOT EXISTS attendees_ai AFTER INSERT ON attendees BEGIN
        INSERT INTO attendees_fts(rowid, email, fullname, company, dietaryreq)
        VALUES (new.id, new.email, new.fullname, new.company, new.dietaryreq);
    END;
    """;
cmd.ExecuteNonQuery();
```

---

## 7. Heartbeat & Health Checks

### 7.1 Heartbeat Publisher

```csharp
public class HeartbeatPublisher : BackgroundService
{
    private readonly IMessageBus _messageBus;
    private readonly IHealthCheckService _health;
    private readonly ILogger _logger;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                var heartbeat = new SystemHeartbeat
                {
                    System = "TicketMasala",
                    InstanceId = Environment.MachineName,
                    Timestamp = DateTime.UtcNow,
                    Status = await _health.CheckStatusAsync() switch
                    {
                        HealthStatus.Healthy => "Healthy",
                        HealthStatus.Degraded => "Degraded",
                        _ => "Critical"
                    },
                    Metrics = new
                    {
                        CpuUsagePercent = GetCpuUsage(),
                        MemoryMbUsed = GC.GetTotalMemory(false) / 1024 / 1024,
                        DatabaseLatencyMs = await MeasureDbLatency(),
                        QueueDepth = GetRabbitMqQueueDepth()
                    }
                };

                await _messageBus.PublishAsync(
                    "monitoring.heartbeats",
                    heartbeat,
                    ct);
            }
        }
        catch (OperationCanceledException) { }
    }
}
```

---

## 8. Idempotency & Deduplication

### 8.1 Idempotent Message Processing

```csharp
public class IngestedMessages
{
    public string MessageId { get; set; }  // PK: Unique per external system
    public string QueueName { get; set; }
    public string Payload { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class RabbitMqIngressService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            var messageId = ea.BasicProperties.MessageId;
            
            // Check if already processed
            var existing = await _db.IngestedMessages
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
            
            if (existing != null)
            {
                _logger.LogWarning("Duplicate message: {MessageId}", messageId);
                await model.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonConvert.DeserializeObject<RegistrationMessage>(body);
                
                // Transform & store
                var workItem = TransformToWorkItem(message);
                await _db.WorkItems.AddAsync(workItem);
                
                // Record ingestion
                await _db.IngestedMessages.AddAsync(new IngestedMessages
                {
                    MessageId = messageId,
                    QueueName = ea.Exchange,
                    Payload = body,
                    ProcessedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync(ct);
                await model.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await model.BasicNackAsync(ea.DeliveryTag, false, true);  // Requeue
            }
        };

        _channel.BasicConsume("shiftfestival.registrations.created", false, consumer);
    }
}
```

---

## 9. Error Handling & Resilience

### 9.1 Retry Strategy (Exponential Backoff)

```csharp
public class ResilientMessagePublisher
{
    public async Task PublishWithRetryAsync<T>(
        string queueName, T message, CancellationToken ct)
    {
        var policy = Policy
            .Handle<RabbitMQClientException>()
            .Or<IOException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                    _logger.LogWarning(
                        "Retry {RetryCount} after {Delay}ms: {Exception}",
                        retryCount, timespan.TotalMilliseconds, outcome.Exception));

        try
        {
            await policy.ExecuteAsync(async () =>
            {
                await _messageBus.PublishAsync(queueName, message, ct);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed after retries. Adding to backlog: {Queue}", queueName);
            
            await _db.DispatchBacklog.AddAsync(new DispatchBacklogEntry
            {
                QueueName = queueName,
                Payload = JsonConvert.SerializeObject(message),
                FailedAt = DateTime.UtcNow,
                NextRetryAt = DateTime.UtcNow.AddMinutes(30),
                RetryCount = 3,
                Status = "Pending"
            });

            await _db.SaveChangesAsync(ct);
            
            // Alert admin
            await _messageBus.PublishAsync(
                "monitoring.alerts",
                new AdminAlert { Severity = "Error", Message = $"Publishing to {queueName} failed" },
                ct);
        }
    }
}
```

### 9.2 Circuit Breaker Pattern

```csharp
services.AddHttpClient<ExternalSystemClient>()
    .AddTransientHttpErrorPolicy()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, timespan) =>
            _logger.LogWarning("Circuit breaker open for {Timespan}", timespan));
```

---

## 10. Integration Checkpoints

### Pre-Development Checklist

- [ ] RabbitMQ instance running (local or Docker)
- [ ] SQLite database file location configured
- [ ] YAML schema validated (use https://www.jsonschemavalidator.net/ for JSON version)
- [ ] Message contract docs shared with all teams
- [ ] GitHub branches created (main, dev, prod, feature/*)
- [ ] CI/CD pipeline scaffolded in GitHub Actions
- [ ] Secrets configured (RabbitMQ URI, API keys)

### Development Milestones

**Week 1-2: Connectivity**
- RabbitMQ client setup
- Inbound/outbound message handlers
- Heartbeat publisher
- Unit tests

**Week 3-4: Workflows**
- YAML config loading
- Dynamic schema generation
- Observer pattern + FacturationObserver
- Bar order endpoint

**Week 5-6: External Integration**
- Receiver/sender stubs for all teams
- Dashboard mockup
- Admin interface

**Week 7-8: Polish & Testing**
- Load testing (badge scans)
- Integration tests
- Documentation
- Deployment pipeline

---

## 11. Deployment & Operations

### Production Checklist

- [ ] WAL mode enabled & verified
- [ ] RabbitMQ clusters configured (HA)
- [ ] Database backups scheduled (hourly)
- [ ] Monitoring queues set up (Elastic Stack or similar)
- [ ] Admin alerts configured (Slack integration)
- [ ] Secrets rotated
- [ ] Load test passed (p95 < 150ms)
- [ ] Team training completed

---

## Glossary

| Term | Definition |
|------|-----------|
| **Idempotency** | Same message processed twice = same result (no duplicates) |
| **WAL Mode** | Write-Ahead Logging; readers don't block writers in SQLite |
| **FTS5** | Full-Text Search for natural language queries |
| **Dead Letter Queue** | Holds messages that failed processing |
| **Circuit Breaker** | Prevents cascade failures when external system is down |
| **Heartbeat** | Periodic "I am alive" signal (1 per second) |
| **Loose Coupling** | Systems don't know internal details of each other |
| **Graceful Degradation** | Continue operating with reduced capability when external system fails |
| **Observer Pattern** | Objects notify others of state changes without direct coupling |
| **MCP** | Model Context Protocol; safe API for AI queries |
