This is the final barrier against the "Generic Monster." If you let 10,000 IoT sensors or a massive SAP sync hit your main API directly, you will DDOS yourself.

The Ingestion Engine must be a fortress of solitude. It cares only about receiving data quickly, acknowledging it, and processing it later.
The Architectural Pattern: "Store and Forward" (CQRS-Lite)

We will decouple Ingestion (Accepting data) from Digestion (Parsing/Validating/Saving data).

1. The Component Topology

We are introducing two new deployment units:

    The Gatekeeper (Ingestion API): A stripped-down, high-performance API.

        Role: Authenticate Source, Validate HMAC, Push to Queue, Return 202 Accepted.

        Logic: Zero business logic. No DB calls (except maybe Redis for rate limiting).

        Tech Stack: .NET 8 Minimal API (AOT Compiled) or Go (if RPS > 10k).

    The Stomach (Worker Service): A background daemon.

        Role: Pull from Queue, Load Domain Config, Transform Payload -> WorkItem, Save to DB.

        Logic: Heavy parsing, retries, mapping.

sequenceDiagram
    participant Source as IoT/Webhook
    participant Gate as Gatekeeper API
    participant Q as Message Bus (RabbitMQ)
    participant Worker as Digestion Worker
    participant DB as Postgres

    Source->>Gate: POST /hooks/gardening/sensors
    Note over Gate: Validate HMAC<br/>Rate Limit Check
    Gate->>Q: Publish (RawPayload)
    Gate-->>Source: 202 Accepted (Fast!)
    
    loop Async Processing
        Worker->>Q: Consume Message
        Worker->>Worker: Load YAML Mapping
        Worker->>Worker: Transform JSON -> WorkItem
        Worker->>DB: Insert WorkItem
    end

2. Configuration: The Ingestion Mapping (YAML)

Just like we mapped ML features, we must map external chaotic JSON to our internal pristine structure.

File: masala_integrations.yaml

domains:
  Gardening:
    ingestion:
      - id: "sensor_network_v1"
        type: "webhook"
        endpoint_suffix: "sensors" # -> /api/hooks/gardening/sensors
        secret_env_var: "GARDEN_SENSOR_SECRET"

        # The Mapping Strategy
        mapping:
          # Universal Fields
          title: "Soil Alert: {{ location_name }}" # String interpolation
          description: "Sensor {{ device_id }} reports {{ reading_type }} of {{ value }}"
          work_item_type: "INCIDENT"
          
          # Custom Field Mapping
          custom_fields:
            - target: "soil_ph"
              source: "value"
              condition: "reading_type == 'ph'" # Only map if this condition matches
            - target: "sensor_id"
              source: "device_id"
              
        # Deduplication Strategy (Crucial for IoT)
        deduplication:
          key_fields: ["device_id", "reading_type"]
          window_seconds: 600 # Ignore duplicates for 10 mins

3. Implementation: The Gatekeeper (High Performance)

This needs to be blazing fast. We use .NET 8 Minimal APIs.

// Program.cs (The Gatekeeper)
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Generic Endpoint: /api/hooks/{domain}/{source}
app.MapPost("/api/hooks/{domain}/{source}", async (
    string domain,
    string source,
    HttpRequest request,
    IMessageBus bus) =>
{
    // 1. FAST SECURITY CHECK
    // Do not load full Domain Config here. Check Redis or Env Vars for secrets.
    if (!await SecurityValidator.ValidateHmacAsync(request, domain))
        return Results.Unauthorized();

    // 2. READ BODY STREAM (Do not parse JSON yet)
    using var reader = new StreamReader(request.Body);
    var rawBody = await reader.ReadToEndAsync();

    // 3. WRAP & PUSH
    var eventMessage = new IngestionEvent 
    {
        DomainId = domain,
        SourceId = source,
        Payload = rawBody,
        ReceivedAt = DateTime.UtcNow
    };
    
    // Fire and forget
    await bus.PublishAsync("ingestion.events", eventMessage);

    // 4. RETURN 202
    return Results.Accepted();
});

app.Run();

4. Implementation: The Digestion Worker (The Mapper)

This is where the complex RuleCompilerService and mapping logic lives.

public class DigestionWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _messageBus.Subscribe("ingestion.events", async (eventMsg) =>
        {
            // 1. Load Domain Config
            var config =_configService.GetDomain(eventMsg.DomainId);
            var mapRules = config.Ingestion.First(x => x.Id == eventMsg.SourceId);

            // 2. Parse Raw Payload
            var sourceJson = JsonDocument.Parse(eventMsg.Payload);
            
            // 3. Check Deduplication (Redis)
            if (await _deduplicator.IsDuplicate(mapRules.Deduplication, sourceJson))
                return; // Skip

            // 4. Map to WorkItem
            var ticket = new WorkItem
            {
                DomainId = eventMsg.DomainId,
                Status = "New",
                // Use a Template Engine (like Scriban) for string interpolation
                Title = _templateEngine.Render(mapRules.Mapping.Title, sourceJson),
                CustomFieldsJson = MapCustomFields(mapRules.Mapping.CustomFields, sourceJson)
            };

            // 5. Save & Trigger Rules
            await _ticketService.CreateTicketAsync(ticket);
        });
    }
}

5. Handling The "Heavy" Syncs (ERP/IMAP)

Webhooks are Push. ERP and Email are Pull. Do not put Polling logic inside the Webhook Worker.

The Poller Pattern: Use Quartz.NET or Hangfire inside the Worker Service.

    Schedule: "Every 15 minutes."

    Action: Connect to SAP/IMAP.

    Optimization: Do not process the data in the Poller job.

        Bad: Poller connects to SAP -> Downloads 1000 records -> Process them -> Save. (Memory spike).

        Good: Poller connects to SAP -> Downloads 1000 records -> Splits them into 1000 messages -> Pushes to ingestion.events Queue.

        Result: The same "Digestion Worker" handles SAP records exactly like Webhook records. Uniformity!

Summary of Decisions

    Split the Stack: Gatekeeper API (Dumb/Fast) + Worker Service (Smart/Slow).

    Unified Pipeline: ERP Pollers simply feed the Queue; they don't process logic.

    Deduplication: Mandatory for IoT to prevent ticket spam. Handled at the Worker level via Redis.

    Security: Validation happens at the Gate, not the Worker.

Final Verification

We have covered the Data (Hybrid), Logic (Compiled), Intelligence (ML Feature Extraction), and Input (Sidecar Ingestion).

You have a complete RFC v1.1.
