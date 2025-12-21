1. The Persistence Layer: Enforcing "SQLite Strict Mode"

Your document mentions SQLite (WAL Mode). That is the bare minimum. To prevent "Table Scans on JSON blobs" (which will kill us once we hit 100k tickets), we are not just dumping JSON into a text column. We are using Generated Columns.

If a user configures a field PriorityScore in masala_domains.yaml, we do not query the JSON blob at runtime. We use EF Core to map a persistent, indexable computed column.

The Mandate:

    WAL Mode: Essential for concurrency.

    Synchronous Normal: Trade a nanosecond of durability for a millisecond of performance.

    JSON Extract: We index specific JSON paths defined in config.

The Implementation Pattern (EF Core):
C#

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // 1. Enforce WAL in the connection string, not just here.

    // 2. The Universal Entity
    modelBuilder.Entity<Ticket>(entity =>
    {
        entity.ToTable("Tickets");
        
        // The JSON Blob
        entity.Property(e => e.CustomFieldsJson).HasColumnType("TEXT");

        // CRITICAL: Generated Columns for High-Speed Filtering
        // We do NOT scan the JSON. We scan this column.
        entity.Property(e => e.ComputedPriority)
              .HasComputedColumnSql("json_extract(CustomFieldsJson, '$.priority_score')", stored: true);
              
        entity.HasIndex(e => e.ComputedPriority); // Index the generated column
    });
}

2. The Logic Layer: "Compile, Don't Interpret"

I see references to a RuleEngineService. Let me be clear: We do not parse strings at runtime.

If masala_domains.yaml has a rule: condition: "ticket.amount > 500", and you use a library to parse that string inside the processing loop, I will reject the PR. String parsing creates garbage collection pressure (GC pauses) and is slow.

The Strategy:

    Startup: Read YAML.

    Compilation: Use System.Linq.Expressions to compile that rule into a Func<Ticket, bool> delegate.

    Runtime: Invoke the delegate. Zero parsing.

The Pattern:
C#

// BAD (The Interpreter Trap):
// public bool Evaluate(Ticket t, string rule) { return _parser.Parse(rule).Execute(t); }

// GOOD (The Expression Tree):
// Stored in a Dictionary<string, Func<Ticket, bool>> _compiledRules;
public void CompileRule(string ruleName, string property, int threshold)
{
    var param = Expression.Parameter(typeof(Ticket), "t");
    var prop = Expression.Property(param, property); // Access property
    var val = Expression.Constant(threshold);
    var body = Expression.GreaterThan(prop, val); // t.Prop > 500

    var lambda = Expression.Lambda<Func<Ticket, bool>>(body, param);
    _compiledRules[ruleName] = lambda.Compile(); // Compiles to IL
}

3. The Ingestion Flow: Store and Forward

We are rejecting RabbitMQ. Excellent. However, System.Threading.Channels must be implemented correctly to ensure backpressure. If the channel fills up, we must decide: do we block the API or drop the request?

For Ticket Masala, we block (or return 503) to protect the worker.
Code snippet

sequenceDiagram
    participant API as Ingestion API (Gatekeeper)
    participant Channel as System.Threading.Channel
    participant Worker as Background Worker
    participant DB as SQLite (WAL)

    Note over API, DB: All in ONE Process
    
    API->>API: Validate Payload (Schema)
    
    alt Channel Full?
        API-->>User: 503 Service Unavailable
    else Channel Open
        API->>Channel: WriteAsync(Payload)
        Channel-->>API: Ack
        API-->>User: 202 Accepted
    end
    
    loop Background Thread
        Worker->>Channel: ReadAsync()
        activate Worker
        Worker->>Worker: Run Compiled Rules
        Worker->>DB: Transactional Write
        deactivate Worker
    end

4. GERDA AI: In-Process Only

Your documentation mentions "Strategy Factory." Ensure this factory returns Singletons where possible.

For the ML models (ONNX/ML.NET), loading the model is expensive.

    Do not load the model per request.

    Do inject PredictionEnginePool<Input, Output> (from Microsoft.Extensions.ML).

We are not calling OpenAI APIs for every ticket. We are running local matrix factorization or regression models to predict estimation.
5. Immediate Next Steps

The documentation is accepted as the baseline, provided the implementation adheres to the strict performance constraints above.

I want to start with the Data Layer because if the schema is wrong, the config engine is useless.

Would you like me to scaffold the TicketDbContext with the SQLite configurations and the MasalaEntity base class to ensure we are correctly implementing the Generated Columns strategy?
