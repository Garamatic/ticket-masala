⚙️ Implementation Specification: DomainConfigService Hot Reload

This specification details the implementation of the Configuration Hot Reload feature (Improvement 1.2) for the DomainConfigService. The core objective is to reload critical YAML configuration files (masala_domains.yaml, masala_gerda.yaml) without requiring an application restart, and crucially, to safely and atomically invalidate and recompile the delegates in the RuleCompilerService. This is required to uphold the "Compile, Don't Interpret" principle.

1. Core Services and Dependencies
1.1 DomainConfigService (The Listener)

This is the primary service responsible for monitoring configuration changes and triggering the reload sequence. It must depend on the Expression Tree compiler service.

    Lifetime: Singleton (AddSingleton)

    Dependencies:

        IOptionsMonitor<DomainConfiguration> (to trigger file change events).

        ILogger<DomainConfigService> (for structured logging).

        RuleCompilerService (to recompile and cache delegates).

        IMemoryCache (to invalidate any configuration-dependent caches).

1.2 RuleCompilerService (The Executor)

This service manages the high-performance, compiled delegates. It must provide a synchronized method for atomic cache replacement.

    Method Signature Update:

C#

public class RuleCompilerService
{
    private ConcurrentDictionary<string, Func<WorkItem, bool>> _compiledRules = new();

    // Existing: Use the cached rules
    public Func<WorkItem, bool> GetRuleDelegate(string ruleKey) { /* ... */ }

    // New: Atomic replacement method for hot reload
    public void ReplaceRuleCache(
        DomainConfiguration newConfiguration, 
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // 1. Compile ALL new rules into a TEMPORARY dictionary
        var newRules = new ConcurrentDictionary<string, Func<WorkItem, bool>>();
        
        // 2. Loop through newConfiguration and compile rules using Expression Trees
        //    (This is the CPU-intensive part, must be thread-safe)
        //    If compilation fails, log and THROW/ABORT. Do NOT proceed.
        
        // 3. ATOMIC SWAP: Use a synchronized replacement or System.Threading.Interlocked
        Interlocked.Exchange(ref _compiledRules, newRules);
        
        logger.LogInformation("Successfully swapped {Count} compiled rule delegates.", newRules.Count);
    }
}

2. Hot Reload Workflow (Sequence Diagram)

The following sequence ensures that the old, stable configuration remains active until the new one is fully compiled and ready.
Code snippet

sequenceDiagram
    participant OS as OS/FileWatcher
    participant App as Program.cs/IOptionsMonitor
    participant DCS as DomainConfigService
    participant RCS as RuleCompilerService
    participant IPH as IngestionPipeline (Hot Path)

    Note over OS,App: masalam_domains.yaml is modified
    OS->>App: File Changed Event
    App->>DCS: IOptionsMonitor.OnChange(newConfig)
    
    activate DCS
    DCS->>DCS: Log: "Reload sequence initiated."
    
    Note over DCS,RCS: Compile, Don't Interpret Mandate
    DCS->>RCS: ReplaceRuleCache(newConfig, CancellationToken)
    
    activate RCS
    RCS->>RCS: 1. Create temporary newRules Dictionary
    RCS->>RCS: 2. Compile ALL Expression Trees (CPU Load)
    
    alt Compilation Successful
        RCS->>RCS: 3. Interlocked.Exchange(ref _compiledRules, newRules)
        Note over RCS: Atomic Swap (Zero Downtime)
        RCS-->>DCS: Success
    else Compilation Failure (YAML error, syntax error)
        RCS->>RCS: Log: "Compilation FAILED. Aborting swap."
        RCS-->>DCS: Failure (Exception)
        deactivate RCS
        DCS->>DCS: Log ERROR. **Current working rules remain active.**
        DCS->>DCS: Alerting Webhooks trigger (5.4)
        DCS-->>IPH: No Change
        return DCS
    end
    
    RCS-->>DCS: Success
    deactivate RCS
    
    DCS->>DCS: Invalidate IMemoryCache entries (Config-dependent)
    DCS->>DCS: Log: "Config and Rules successfully reloaded and active."
    
    IPH->>RCS: GetRuleDelegate(key)
    RCS-->>IPH: New Delegate

3. Implementation Details and Constraints
3.1 Error Handling (Safety First)

The single most critical constraint is that a compilation failure must never replace a working rule set.

    Principle: If RuleCompilerService.ReplaceRuleCache throws an exception (e.g., Expression Tree generation fails due to a bad YAML rule syntax), the Interlocked.Exchange step must not be reached. The old, stable configuration and delegates remain in use, and an immediate error must be logged (Structured Logging) and an alert raised (Alerting Webhooks).

3.2 Thread Safety

The ReplaceRuleCache method must be thread-safe.

    The CPU-intensive compilation step (Step 2) must run without locks on the main _compiledRules dictionary, as the Ingestion Pipeline (Hot Path) must continue using the old delegates unimpeded.

    The use of Interlocked.Exchange ensures that the swap of the dictionary reference is a single atomic operation, preventing race conditions where the Hot Path might read a partially updated dictionary.

3.3 Dependencies on DomainConfigVersion

While the reload is driven by IOptionsMonitor file changes, the DomainConfigService must immediately capture and persist a new DomainConfigVersion entity after a successful rule compilation and swap.

    Rationale: This links the new active configuration to the current database state, upholding the principle that every system state (and every trained ML Model) is linked to a specific configuration snapshot.
