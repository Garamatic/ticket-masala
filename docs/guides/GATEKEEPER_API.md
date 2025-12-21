# Gatekeeper API: Scalable Ingestion Service

**A minimal API for high-throughput data ingestion**

---

## 🎯 Purpose

The **Gatekeeper API** is a separate, lightweight service designed for **scalable ingestion** of work items (tickets) into Ticket Masala. It provides:

- ✅ **Async processing** via `System.Threading.Channels`
- ✅ **High throughput** for bulk imports
- ✅ **Decoupled architecture** (can scale independently)
- ✅ **Simple API** (single POST endpoint)

---

## 🏗️ Architecture

```
┌─────────────────┐
│  External       │
│  Systems        │
│  (CSV, Email,   │
│   Webhooks)     │
└────────┬────────┘
         │ POST /api/ingest
         ▼
┌─────────────────┐
│  Gatekeeper API │
│  (Minimal API)  │
│                 │
│  ┌───────────┐  │
│  │  Queue    │  │
│  │ (Channel) │  │
│  └─────┬─────┘  │
│        │        │
│  ┌─────▼─────┐  │
│  │  Worker   │  │
│  │ (Background)│
│  └───────────┘  │
└─────────────────┘
         │
         │ Process & Transform
         ▼
┌─────────────────┐
│  Ticket Masala  │
│  Main App       │
│  (Database)     │
└─────────────────┘
```

---

## 🔧 Components

### 1. IngestionQueue<T>

**Purpose:** In-memory queue using `System.Threading.Channels`

```csharp
public class IngestionQueue<T>
{
    private readonly Channel<T> _queue;
    
    public async ValueTask EnqueueAsync(T item)
    {
        await _queue.Writer.WriteAsync(item);
    }
    
    public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
```

**Characteristics:**
- ✅ Unbounded channel (no size limit)
- ✅ Thread-safe
- ✅ High performance (faster than external message queues)
- ✅ In-memory (no network overhead)

### 2. IngestionWorker

**Purpose:** Background service that processes queued items

```csharp
public class IngestionWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var payload = await _queue.DequeueAsync(stoppingToken);
            // Process payload
            // Transform to Ticket
            // Save to database
        }
    }
}
```

**Responsibilities:**
- Dequeue items from channel
- Transform payload to domain model
- Validate data
- Save to Ticket Masala database

### 3. API Endpoint

**Endpoint:** `POST /api/ingest`

**Request:**
```http
POST /api/ingest
Content-Type: application/json

{
  "title": "Service Request",
  "description": "Need help with...",
  "domainId": "IT",
  "customFields": { ... }
}
```

**Response:**
```http
HTTP/1.1 202 Accepted
```

**Behavior:**
- Accepts payload immediately (non-blocking)
- Enqueues to channel
- Returns 202 Accepted
- Worker processes asynchronously

---

## 🚀 Usage

### Starting Gatekeeper API

```bash
# Standalone
dotnet run --project src/GatekeeperApi

# Or via Docker
docker build -t gatekeeper-api .
docker run -p 5000:8080 gatekeeper-api
```

### Sending Data

```bash
curl -X POST http://localhost:5000/api/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "title": "New Ticket",
    "description": "Description here",
    "domainId": "IT"
  }'
```

### Integration Example

```csharp
// From external system
var client = new HttpClient();
var payload = new
{
    title = "Service Request",
    description = "Need assistance",
    domainId = "IT",
    customFields = new { priority = "High" }
};

var response = await client.PostAsJsonAsync(
    "http://gatekeeper-api:5000/api/ingest",
    payload
);

// Returns immediately (202 Accepted)
// Processing happens in background
```

---

## 🎯 Use Cases

### 1. Bulk CSV Import

```csharp
// Read CSV file
var records = ReadCsv("import.csv");

// Send each record to Gatekeeper
foreach (var record in records)
{
    await client.PostAsJsonAsync("/api/ingest", record);
}

// All records queued instantly
// Worker processes them asynchronously
```

### 2. Email Ingestion

```csharp
// Email webhook receives email
public async Task<IActionResult> EmailWebhook(EmailMessage email)
{
    // Forward to Gatekeeper
    await _gatekeeperClient.PostAsJsonAsync("/api/ingest", new
    {
        title = email.Subject,
        description = email.Body,
        domainId = DetectDomain(email),
        source = "email"
    });
    
    return Accepted(); // Non-blocking
}
```

### 3. External System Integration

```csharp
// External system sends webhook
public async Task Webhook(ExternalSystemEvent evt)
{
    // Transform to Ticket Masala format
    var ticket = Transform(evt);
    
    // Queue via Gatekeeper
    await _gatekeeperClient.PostAsJsonAsync("/api/ingest", ticket);
}
```

---

## ⚙️ Configuration

### Current Implementation

**Status:** 🟡 **Placeholder/Stub**

The current implementation is a **minimal skeleton**:

```csharp
// Current: Just logs the payload
_logger.LogInformation("Processing ingested payload: {Payload}", payload);
// TODO: Actual processing logic
```

### Planned Implementation

**Phase 8** (from architecture docs):

1. **Payload Transformation**
   - Parse JSON payload
   - Map to Ticket domain model
   - Apply domain-specific rules

2. **Validation**
   - Validate against domain configuration
   - Check custom fields
   - Apply business rules

3. **Database Integration**
   - Connect to Ticket Masala database
   - Create Ticket entities
   - Trigger GERDA AI processing

4. **Error Handling**
   - Retry logic
   - Dead letter queue
   - Error notifications

---

## 🔄 Integration with Main App

### Option 1: Shared Database

Gatekeeper API connects to same database as main app:

```csharp
// Gatekeeper API
services.AddDbContext<MasalaDbContext>(options =>
    options.UseSqlite(connectionString));

// Worker processes and saves directly
var ticket = new Ticket { ... };
await _context.Tickets.AddAsync(ticket);
await _context.SaveChangesAsync();
```

### Option 2: API Call

Gatekeeper API calls main app's API:

```csharp
// Worker processes payload
var ticket = TransformPayload(payload);

// Call main app API
await _httpClient.PostAsJsonAsync(
    "http://main-app:8080/api/tickets",
    ticket
);
```

### Option 3: Shared Channel (Future)

Both apps share the same channel/queue:

```csharp
// Gatekeeper enqueues
await _sharedQueue.EnqueueAsync(payload);

// Main app worker processes
var payload = await _sharedQueue.DequeueAsync();
```

---

## 📊 Performance Characteristics

### Throughput

- **Channel-based:** ~100k+ messages/second (in-memory)
- **API endpoint:** Limited by HTTP overhead
- **Worker:** Limited by database write speed

### Scalability

- **Horizontal:** Run multiple Gatekeeper instances
- **Worker scaling:** Multiple workers per instance
- **Database:** Shared database becomes bottleneck

### Latency

- **Enqueue:** < 1ms (in-memory)
- **Processing:** Depends on payload complexity
- **End-to-end:** Typically < 100ms for simple tickets

---

## 🛠️ Development Roadmap

### Phase 1: Current (Stub)
- ✅ Basic queue implementation
- ✅ Background worker skeleton
- ✅ Minimal API endpoint
- ⚠️ No actual processing

### Phase 2: Basic Processing
- [ ] Payload parsing
- [ ] Ticket creation
- [ ] Database integration
- [ ] Error handling

### Phase 3: Advanced Features
- [ ] Payload transformation (Scriban templates)
- [ ] Domain detection
- [ ] Custom field mapping
- [ ] GERDA AI integration

### Phase 4: Production Ready
- [ ] Retry logic
- [ ] Dead letter queue
- [ ] Metrics/observability
- [ ] Rate limiting

---

## 🔍 Comparison: Gatekeeper vs Main App Ingestion

| Aspect | Gatekeeper API | Main App Ingestion |
|--------|---------------|-------------------|
| **Purpose** | Bulk/high-throughput | Interactive/user-driven |
| **Architecture** | Async queue | Synchronous |
| **Latency** | Accepts immediately | Blocks until saved |
| **Throughput** | High (100k+/sec) | Lower (limited by DB) |
| **Use Case** | CSV imports, webhooks | User creates ticket |
| **Scaling** | Can scale independently | Scales with main app |

---

## 💡 Best Practices

### When to Use Gatekeeper API

✅ **Use Gatekeeper when:**
- Bulk imports (CSV, Excel)
- External system webhooks
- Email ingestion
- High throughput needed
- Non-blocking ingestion required

### When to Use Main App API

✅ **Use Main App API when:**
- User creates ticket interactively
- Need immediate response
- Complex validation required
- Need to trigger UI updates

---

## 📚 Related Documentation

- [Ingestion Architecture](../architecture/DETAILED.md#ingestion)
- [Tenants vs Domains](TENANTS_VS_DOMAINS.md)
- [Configuration Guide](CONFIGURATION.md)

---

## 🔗 Code References

- **Implementation:** `src/GatekeeperApi/`
- **Queue:** `src/GatekeeperApi/IngestionComponents.cs`
- **Main App Ingestion:** `src/TicketMasala.Web/Engine/Ingestion/`

---

**Last Updated:** January 2025

