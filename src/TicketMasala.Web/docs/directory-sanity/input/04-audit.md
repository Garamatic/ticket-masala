1. The Data Layer: Verify the "Generated Column" Contract

You mention src/TicketMasala.Web/Data/. You claim "High-traffic fields are mapped to generated columns."

The Trap: Many developers define the column in SQL but forget to map it in EF Core, causing the app to read the full JSON blob into memory to filter it.

The Mandate: Your MasalaDbContext.cs inside Data/ must look exactly like this for the WorkItem entity. If you are missing HasComputedColumnSql, you are failing.
C#

modelBuilder.Entity<WorkItem>(entity =>
{
    // The JSON Blob
    entity.Property(e => e.Payload).HasColumnType("TEXT");

    // VITAL: The Virtual Column for Indexing
    // This allows SQLite to index 'Status' without duplicating data on disk
    entity.Property(e => e.Status)
          .HasComputedColumnSql("json_extract(Payload, '$.Status')", stored: false); // Virtual
          
    entity.HasIndex(e => e.Status); // The Index that saves us from Table Scans
});

2. The Ingestion Engine: "Store and Forward" Visualization

Inside src/TicketMasala.Web/Engine/Ingestion/, you have Background/.

The Trap: Using Task.Run inside the Controller. The Mandate: The Controller must strictly Write to the Channel. The Background Service must strictly Read. There is no direct bridge.

Here is the flow I expect to see implemented in Ingestion:
Code snippet

sequenceDiagram
    participant API as IngestionController
    participant Channel as Channel<WorkItem>
    participant Worker as BackgroundProcessor
    participant DB as SQLite (WAL)

    Note over API: Client POSTs Ticket
    API->>API: Validate Basic Schema (Fast)
    API->>Channel: TryWrite(Ticket)
    
    alt Channel Full?
        Channel-->>API: False
        API-->>Client: 429 Too Many Requests
    else Channel Open
        Channel-->>API: True
        API-->>Client: 202 Accepted
    end

    loop Background Thread
        Worker->>Channel: ReadAsync()
        Worker->>Worker: Run GERDA Rules (Compiled)
        Worker->>DB: Batch Insert (Transaction)
    end

3. The Compiler Core: The "No Reflection" Zone

You created src/TicketMasala.Web/Engine/Compiler/. This is the kill zone.

The Trap: Parsing a string like "ticket.Priority > 10" using a library that uses Reflection at runtime. The Mandate: You must use System.Linq.Expressions.

If your RuleCompilerService parses the rule every time a ticket is processed, we lose.

    Startup: Parse YAML → Expression Tree → Func<WorkItem, bool>.

    Runtime: Invoke Func.