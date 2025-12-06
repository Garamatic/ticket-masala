Entity Relationship Diagram (ERD) for the Ticket Masala RFC 1.0.

This schema specifically addresses your Performance needs (Indexing) and the Versioning safety net we discussed.
Key Architectural Decisions in this Schema:

    The "Snapshot" Strategy: Notice WorkItem links to DomainConfigVersion, not the live Domain. This freezes the rules (SLA, Transitions) at the moment the ticket was created/updated, solving the "In-Flight" paradox.

    Hybrid Storage: CustomFields is explicitly marked as JSONB. In PostgreSQL, this is a binary, indexable column. In SQL Server, this is NVARCHAR(MAX) and requires computed columns for indexing.

    Recursive Hierarchy: WorkContainer uses a self-referencing ParentContainerId.

        Warning: In SQL, querying deep trees (e.g., "Show me all tickets in this Portfolio hierarchy") requires Recursive CTEs (Common Table Expressions). If the tree gets deeper than 5 levels, we must switch this specific table to a Graph Database (Neo4j) or use a HierarchyId data type (SQL Server specific).

        ```mermaid
        erDiagram
    %% CORE TENANCY & CONFIGURATION
    %% ---------------------------------------------------
    Tenant ||--|{ Domain : "owns"
    Domain ||--|{ DomainConfigVersion : "has history of"
    
    Tenant {
        uuid Id PK
        string Name
        string PlanType
        bool IsActive
    }

    Domain {
        string Id PK "e.g., 'IT', 'HR'"
        uuid TenantId FK
        string DisplayName
        uuid CurrentConfigVersionId FK "Pointer to live config"
    }

    DomainConfigVersion {
        uuid Id PK
        string DomainId FK
        string VersionHash "SHA256 of YAML"
        int VersionNumber
        jsonb FullConfigBlob "The parsed YAML as JSON"
        datetime CreatedAt
        string CreatedBy
    }

    %% WORK STRUCTURE (HIERARCHY)
    %% ---------------------------------------------------
    WorkContainer ||--o{ WorkContainer : "parent of"
    WorkContainer ||--|{ WorkItem : "contains"
    
    WorkContainer {
        uuid Id PK
        uuid TenantId FK
        uuid ParentContainerId FK "Adjacency List Pattern"
        string Name
        string Type "e.g., Project, Zone"
        jsonb CustomFields "Context specific data"
    }

    %% THE CORE ENTITY (TICKET)
    %% ---------------------------------------------------
    WorkItem ||--|| DomainConfigVersion : "bound to schema version"
    
    WorkItem {
        uuid Id PK
        uuid TenantId FK
        string DomainId FK
        
        %% The Snapshot Link
        uuid ConfigVersionId FK "Crucial: The rules applied to this ticket"
        
        %% Universal Fields (Indexed)
        string Status
        string Title
        timestamp CreatedAt
        timestamp DueDate
        int EstimatedEffort
        float PriorityScore
        
        %% The Dynamic Payload
        jsonb CustomFields "GIN Indexed in Postgres"
        
        %% Relationships
        uuid ContainerId FK
        uuid AssigneeId
    }

    %% INTEGRATION & LOGS
    %% ---------------------------------------------------
    WorkItem ||--|{ WorkItemAudit : "history"
    
    WorkItemAudit {
        uuid Id PK
        uuid WorkItemId FK
        string Action "Transition, Edit, Comment"
        string FromState
        string ToState
        jsonb ChangeSet "Diff of changes"
        timestamp Timestamp
    }
        ```

Critical Implementation Notes

1. Handling the JSONB Column (PostgreSQL vs SQL Server)

To ensure you don't build a "Generic Monster" that can't be queried, you must implement the Indexing Strategy in your Entity Framework (EF) Core configuration.

If using PostgreSQL (Recommended for this stack):

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Create a GIN index on the CustomFields JSONB column
    // This allows fast queries like: WHERE custom_fields @> '{"soil_ph": 7}'
    modelBuilder.Entity<WorkItem>()
        .HasIndex(b => b.CustomFields)
        .HasMethod("gin");
}
```

If using SQL Server (Constraint): You cannot index the JSON blob directly. You must identify "High Value" fields and promote them to Computed Columns.

-- SQL Migration Script
ALTER TABLE WorkItems
ADD SoilPh AS CAST(JSON_VALUE(CustomFields, '$.soil_ph') AS FLOAT) PERSISTED;

CREATE INDEX IX_WorkItems_SoilPh ON WorkItems(SoilPh);

2. The Hierarchy Warning (WorkContainer)

You are using a self-referencing table (ParentContainerId).

    The Problem: If a user asks "Get all Tickets for Portfolio X", and Portfolio X has Projects, which have Sub-Projects, which have Phases... SQL struggles.

    The limit: If your WorkContainer depth exceeds 4-5 levels, standard JOINs fail.

    Mitigation: For V1, stick to SQL but use CTE (Common Table Expressions) for the read. If performance dips, we move only the WorkContainer table to a Graph Database or cache the "Tree Path" (Materialized Path Pattern: Root/Child/Grandchild) in a string column.

This is the critical piece that prevents the "Generic Monster" from eating your CPU.

We are moving from Runtime Interpretation (slow, iterating loops, string parsing every time) to Compile-Time Generation (fast, native IL code).
The Concept: Expression Trees

We will take the YAML config and "write code" in memory, then compile it into a delegate (Func<WorkItem, bool>). To the CLR, this looks exactly as if you had hardcoded the logic in C#.
The Helper: JSON Extraction

Expression trees dealing with raw JSON libraries (System.Text.Json) are notoriously verbose. To keep this readable, we will use a Static Helper Method that the Expression Tree will call to extract values.

1. The Rule Models (DTOs)

public enum RuleOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    Contains
}

public class RuleDefinition
{
    public string FieldKey { get; set; } // e.g., "soil_ph"
    public string FieldType { get; set; } // "string", "number", "bool"
    public RuleOperator Operator { get; set; }
    public string TargetValue { get; set; } // e.g., "6.5"
}

2. The Compiler Service

This service is a Singleton. It compiles rules once (at startup or config reload) and caches them.

using System.Linq.Expressions;
using System.Text.Json;

public class RuleCompilerService
{
    // CACHE: Domain -> State -> Compiled Delegate
    private readonly Dictionary<string, Dictionary<string, Func<WorkItem, bool>>> _ruleCache
        = new();

    /// <summary>
    /// Compiles a list of YAML rules into a single high-performance delegate.
    /// Effectively generates: ticket => Check(ticket.CustomFields, "field") > 100
    /// </summary>
    public Func<WorkItem, bool> CompileRules(List<RuleDefinition> rules)
    {
        // 1. The Input Parameter (t => ...)
        var workItemParam = Expression.Parameter(typeof(WorkItem), "t");

        // Start with "true" (Identity for AND operations)
        Expression combinedExpression = Expression.Constant(true);

        foreach (var rule in rules)
        {
            // 2. Build the Left Side: Extract value from JSON
            // Calls: FieldExtractor.GetNumber(t.CustomFields, "soil_ph")
            Expression left = BuildExtractionExpression(workItemParam, rule);

            // 3. Build the Right Side: The Target Value from Config
            Expression right = BuildConstantExpression(rule.TargetValue, rule.FieldType);

            // 4. Build the Comparison: (> , < , ==)
            Expression comparison = BuildComparison(left, right, rule.Operator);

            // 5. Combine with AND (&&)
            combinedExpression = Expression.AndAlso(combinedExpression, comparison);
        }

        // 6. Compile to Func<WorkItem, bool>
        return Expression.Lambda<Func<WorkItem, bool>>(combinedExpression, workItemParam).Compile();
    }

    private Expression BuildExtractionExpression(ParameterExpression param, RuleDefinition rule)
    {
        // Access t.CustomFields (property)
        var customFieldsProp = Expression.Property(param, nameof(WorkItem.CustomFields));
        var keyConst = Expression.Constant(rule.FieldKey);

        // Determine which helper method to call based on type
        string methodName = rule.FieldType.ToLower() switch
        {
            "number" => nameof(FieldExtractor.GetNumber), // returns double
            "bool"   => nameof(FieldExtractor.GetBool),   // returns bool
            _        => nameof(FieldExtractor.GetString)  // returns string
        };

        var methodInfo = typeof(FieldExtractor).GetMethod(methodName);
        
        // Generates: FieldExtractor.GetNumber(t.CustomFields, "key")
        return Expression.Call(methodInfo, customFieldsProp, keyConst);
    }

    private Expression BuildConstantExpression(string value, string type)
    {
        return type.ToLower() switch
        {
            "number" => Expression.Constant(double.Parse(value)),
            "bool"   => Expression.Constant(bool.Parse(value)),
            _        => Expression.Constant(value)
        };
    }

    private Expression BuildComparison(Expression left, Expression right, RuleOperator op)
    {
        return op switch
        {
            RuleOperator.Equals => Expression.Equal(left, right),
            RuleOperator.NotEquals => Expression.NotEqual(left, right),
            RuleOperator.GreaterThan => Expression.GreaterThan(left, right),
            RuleOperator.LessThan => Expression.LessThan(left, right),
            RuleOperator.Contains => Expression.Call(left, typeof(string).GetMethod("Contains", new[] { typeof(string) }), right),
            _ => throw new NotImplementedException($"Operator {op} not supported")
        };
    }
}

3. The Extraction Helper (Static)

This isolates the messy System.Text.Json logic from the Expression Tree complexity.

public static class FieldExtractor
{
    // NOTE: In production, optimize this to avoid re-parsing JSON if possible.
    // Ideally, WorkItem.CustomFields is already a JsonElement or Dictionary.

    public static double GetNumber(string json, string key)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetDouble();
        }
        return 0; // Default or throw based on preference
    }

    public static string GetString(string json, string key)
    {
        if (string.IsNullOrEmpty(json)) return null;
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(key, out var prop))
        {
            return prop.ToString();
        }
        return null;
    }
    
    public static bool GetBool(string json, string key)
    {
        // Similar implementation...
        return false; 
    }
}

Why This Wins

    Speed: Once compiled (at startup), the Func is cached. Evaluating it is as fast as native C#.

    Safety: If you misconfigure a type (comparing a String > Number), CompileRules will throw an exception at startup (or config reload), not when the user clicks a button. This is "Fail Fast."

    Debuggability: You can unit test the compiler. If the compiler works, every rule it generates works.
