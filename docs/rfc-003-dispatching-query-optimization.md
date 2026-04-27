# RFC: Optimize GERDA Dispatching Database Queries

**Status**: Proposed  
**Author**: AI Architecture Review  
**Date**: 2025-04-27  
**Related**: `MatrixFactorizationDispatchingStrategy.cs`, `EfCoreTicketRepository.cs`, `AgentMatchingEngine.cs`  
**Impact**: Performance (N+1 elimination, ~90% query reduction)

---

## Summary

The GERDA dispatching system suffers from **N+1 query problems** where customer data and FTS5 skill matches are queried **once per agent** inside a loop. For 50 agents, this generates 100+ database round trips. This RFC proposes query batching, pre-loading, and caching to reduce this to **~5 queries total**.

---

## Current Performance Problem

### N+1 Anti-Pattern Evidence

**File**: `MatrixFactorizationDispatchingStrategy.cs` (lines ~130-180)

```csharp
public async Task<List<DispatchResult>> GetRecommendedAgentsAsync(Ticket ticket, int count)
{
    var employees = await _context.Users.OfType<Employee>().ToListAsync(); // Query #1
    
    // Get ticket rowid (for FTS) - Query #2
    long ticketRowId = 0;
    var rowIds = await _context.Database.SqlQueryRaw<long>(
        "SELECT rowid FROM Tickets WHERE Id = {0}", ticket.Guid).ToListAsync();
    
    foreach (var employee in employees)  // LOOP STARTS
    {
        // ❌ Query #3 per agent: Get customer (SAME customer every time!)
        var customer = await _context.Users.FindAsync(ticket.CreatorGuid.ToString());
        
        // ❌ Query #4 per agent: FTS5 skill match
        if (ticketRowId > 0 && !string.IsNullOrWhiteSpace(employee.Specializations))
        {
            var ranks = await _context.Database.SqlQueryRaw<double>(
                "SELECT rank FROM Tickets_Search WHERE rowid = {0} AND Tickets_Search MATCH {1}",
                ticketRowId, matchQuery).ToListAsync();  // Per agent!
        }
        
        // ❌ Query #5 per agent: Workload calculation (can be batched)
        var currentWorkload = agentWorkloads.GetValueOrDefault(employee.Id, 0);
        // agentWorkloads is already batched above, but recalculated per agent logic
    }
}
```

### Query Count Analysis

| Scenario | Current Queries | Optimized Queries | Reduction |
|----------|-----------------|-------------------|-----------|
| 10 agents | ~23 | ~5 | 78% |
| 50 agents | ~103 | ~5 | 95% |
| 100 agents | ~203 | ~5 | 98% |

### Performance Impact

With 50 agents and typical 20ms query latency:
- **Current**: ~2 seconds (103 sequential queries)
- **Optimized**: ~100ms (5 parallel/batched queries)
- **User Impact**: Dispatching feels instant vs. noticeable delay

---

## Root Causes

1. **Customer query inside loop**: Same customer loaded repeatedly
2. **FTS5 per-agent queries**: Could use `IN` clause or batch
3. **No change tracking disable**: Read-only queries using tracking
4. **Missing indexes**: No covering index for workload calculation
5. **Sequential execution**: Agent scoring not parallelized

---

## Proposed Optimizations

### Optimization 1: Pre-Load Customer Data (Critical)

**Before**:
```csharp
foreach (var employee in employees)
{
    var customer = await _context.Users.FindAsync(ticket.CreatorGuid.ToString()); // ❌ N queries
}
```

**After**:
```csharp
// Load ONCE before loop
var customer = await _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Id == ticket.CreatorGuid.ToString());

foreach (var employee in employees)
{
    // Use cached customer reference
    var languageScore = AffinityScoring.CalculateLanguageScore(employee, customer);
}
```

### Optimization 2: Batch FTS5 Queries

**Before**:
```csharp
foreach (var employee in employees)
{
    var specs = JsonSerializer.Deserialize<List<string>>(employee.Specializations);
    var matchQuery = string.Join(" OR ", specs.Select(s => $"\"{s}\""));
    
    // One query per agent
    var ranks = await _context.Database.SqlQueryRaw<double>(
        "SELECT rank FROM Tickets_Search WHERE rowid = {0} AND Tickets_Search MATCH {1}",
        ticketRowId, matchQuery).ToListAsync();
}
```

**After**:
```csharp
// Build combined query for all agents at once
var agentSpecs = employees.ToDictionary(
    e => e.Id,
    e => JsonSerializer.Deserialize<List<string>>(e.Specializations) ?? new List<string>()
);

// Single query with multiple MATCH conditions
var ftsQuery = $@"
    SELECT employee_id, rank 
    FROM (
        {string.Join(" UNION ALL ", agentSpecs.Select((kv, idx) => $@"
            SELECT {idx} as employee_id, rank 
            FROM Tickets_Search 
            WHERE rowid = {ticketRowId} 
            AND Tickets_Search MATCH '{string.Join(" OR ", kv.Value.Select(s => s.Replace("'", "''")))}'
        "))}
    ) combined";

var ftsResults = await _context.Database.SqlQueryRaw<FtsResult>(ftsQuery).ToListAsync();
var ftsScoresByAgent = ftsResults.ToDictionary(r => r.EmployeeId, r => r.Rank);
```

**Alternative (Simpler)**:
```csharp
// Pre-compute ticket category once
var ticketCategory = AffinityScoring.ExtractCategoryFromTicket(ticket);

// Skip FTS entirely if we already have category
// Use string matching instead (faster, no SQL)
foreach (var employee in employees)
{
    var specs = JsonSerializer.Deserialize<List<string>>(employee.Specializations);
    var ftsScore = specs?.Any(s => s.Equals(ticketCategory, StringComparison.OrdinalIgnoreCase)) == true ? 1.0 : 0.0;
}
```

### Optimization 3: Disable Change Tracking

**Before**:
```csharp
var employees = await _context.Users.OfType<Employee>().ToListAsync();
var agentWorkloads = await _context.Tickets
    .Where(t => t.ResponsibleId != null)
    .GroupBy(t => t.ResponsibleId)
    .Select(g => new { AgentId = g.Key!, Count = g.Count() })
    .ToDictionaryAsync(x => x.AgentId!, x => x.Count);
```

**After**:
```csharp
// Add AsNoTracking() to all read-only queries
var employees = await _context.Users
    .AsNoTracking()  // ✅ No tracking overhead
    .OfType<Employee>()
    .ToListAsync();

var agentWorkloads = await _context.Tickets
    .AsNoTracking()  // ✅ No tracking overhead
    .Where(t => t.ResponsibleId != null)
    .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
    .GroupBy(t => t.ResponsibleId)
    .Select(g => new { AgentId = g.Key!, Count = g.Count() })
    .ToDictionaryAsync(x => x.AgentId!, x => x.Count);
```

### Optimization 4: Add Database Indexes

**Migration**:
```csharp
// New migration: Add covering index for workload queries
migrationBuilder.CreateIndex(
    name: "IX_Tickets_ResponsibleId_Status_Workload",
    table: "Tickets",
    columns: new[] { "ResponsibleId", "TicketStatus" });

// Add index for customer lookups
migrationBuilder.CreateIndex(
    name: "IX_Tickets_CreatorGuid",
    table: "Tickets",
    column: "CreatorGuid");
```

**Note**: SQLite FTS5 virtual table (`Tickets_Search`) already has optimized index.

### Optimization 5: Parallel Agent Scoring

**Before**:
```csharp
var scoredAgents = new List<DispatchResult>();
foreach (var employee in employees)  // Sequential
{
    var score = ScoreAgent(employee, ticket, customer);
    scoredAgents.Add(new DispatchResult(employee.Id, score));
}
```

**After**:
```csharp
// Parallel scoring for CPU-bound calculations
var scoredAgents = await Task.WhenAll(
    employees.Select(async employee =>
    {
        // CPU-bound: Score calculation
        var score = ScoreAgent(employee, ticket, customer, agentWorkloads);
        return new DispatchResult(employee.Id, score);
    })
);

// Or using Parallel.ForEach for pure CPU work
var results = new ConcurrentBag<DispatchResult>();
Parallel.ForEach(employees, employee =>
{
    var score = ScoreAgent(employee, ticket, customer, agentWorkloads);
    results.Add(new DispatchResult(employee.Id, score));
});
```

### Optimization 6: Caching for Repeated Calls

**Problem**: Same ticket dispatched multiple times in short window

**Solution**: Memory cache for scoring results

```csharp
public class CachedDispatchingService : IDispatchingService
{
    private readonly IDispatchingService _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public async Task<List<DispatchResult>> GetTopRecommendedAgentsAsync(Guid ticketGuid, int count)
    {
        var cacheKey = $"dispatch:{ticketGuid}:{count}";
        
        if (_cache.TryGetValue(cacheKey, out List<DispatchResult>? cached))
        {
            return cached!;
        }

        var results = await _inner.GetTopRecommendedAgentsAsync(ticketGuid, count);
        
        _cache.Set(cacheKey, results, _cacheDuration);
        return results;
    }
}
```

---

## Refactored Implementation

### New OptimizedDispatchingStrategy

```csharp
public class OptimizedDispatchingStrategy : IDispatchingStrategy
{
    private readonly MasalaDbContext _context;
    private readonly PredictionEnginePool<AgentCustomerRating, RatingPrediction> _pool;
    private readonly IFeatureExtractor _featureExtractor;
    private readonly ILogger<OptimizedDispatchingStrategy> _logger;

    public async Task<List<DispatchResult>> GetRecommendedAgentsAsync(Ticket ticket, int count)
    {
        // STEP 1: Batch load all data (3 queries total)
        var (employees, customer, agentWorkloads) = await LoadDispatchingDataAsync(ticket);

        if (employees.Count == 0)
        {
            _logger.LogWarning("GERDA-D: No employees found");
            return new List<DispatchResult>();
        }

        // STEP 2: Pre-compute ticket features (once)
        var ticketFeatures = _featureExtractor.ExtractFeatures(ticket);
        var ticketCategory = ExtractCategory(ticket);

        // STEP 3: Score all agents (parallel CPU, no DB calls)
        var scoredAgents = ScoreAgentsParallel(
            employees, 
            ticket, 
            customer, 
            agentWorkloads, 
            ticketFeatures,
            ticketCategory);

        // STEP 4: Return top N
        return scoredAgents
            .OrderByDescending(x => x.Score)
            .Take(count)
            .ToList();
    }

    private async Task<(List<Employee> Employees, ApplicationUser? Customer, Dictionary<string, int> Workloads)> 
        LoadDispatchingDataAsync(Ticket ticket)
    {
        // Query 1: Get all employees
        var employeesTask = _context.Users
            .AsNoTracking()
            .OfType<Employee>()
            .ToListAsync();

        // Query 2: Get customer (single lookup)
        var customerTask = _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == ticket.CreatorGuid.ToString());

        // Query 3: Get all agent workloads (single aggregated query)
        var workloadsTask = _context.Tickets
            .AsNoTracking()
            .Where(t => t.ResponsibleId != null)
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .GroupBy(t => t.ResponsibleId)
            .Select(g => new { AgentId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.AgentId!, x => x.Count);

        // Execute in parallel (independent queries)
        await Task.WhenAll(employeesTask, customerTask, workloadsTask);

        return (await employeesTask, await customerTask, await workloadsTask);
    }

    private List<DispatchResult> ScoreAgentsParallel(
        List<Employee> employees,
        Ticket ticket,
        ApplicationUser? customer,
        Dictionary<string, int> workloads,
        float[] ticketFeatures,
        string ticketCategory)
    {
        var results = new ConcurrentBag<DispatchResult>();
        
        var maxWorkload = 15; // From config

        Parallel.ForEach(employees, employee =>
        {
            if (string.IsNullOrEmpty(employee.Id))
                return;

            var workload = workloads.GetValueOrDefault(employee.Id, 0);
            if (workload >= maxWorkload)
                return;

            // CPU-bound: ML prediction (using pool - no DB)
            var mlScore = PredictAffinity(employee.Id, ticket.CreatorGuid.ToString());

            // CPU-bound: Skill matching (no DB)
            var skillScore = CalculateSkillMatch(employee, ticketCategory);

            // CPU-bound: Language/Geo (no DB)
            var languageScore = CalculateLanguageScore(employee, customer);
            var geoScore = CalculateGeoScore(employee, customer);

            // Combine scores
            var finalScore = CombineScores(mlScore, skillScore, languageScore, geoScore, workload);

            var result = new DispatchResult(employee.Id, finalScore);
            AddReasons(result, mlScore, skillScore, languageScore, geoScore, workload);
            
            results.Add(result);
        });

        return results.ToList();
    }

    private float PredictAffinity(string agentId, string customerId)
    {
        try
        {
            var input = new AgentCustomerRating 
            { 
                AgentId = agentId, 
                CustomerId = customerId 
            };
            var prediction = _pool.Predict("GerdaDispatchModel", input);
            return prediction.Score;
        }
        catch
        {
            return 2.5f; // Neutral fallback
        }
    }

    private double CalculateSkillMatch(Employee employee, string ticketCategory)
    {
        if (string.IsNullOrWhiteSpace(employee.Specializations))
            return 0;

        try
        {
            var specs = JsonSerializer.Deserialize<List<string>>(employee.Specializations);
            return specs?.Any(s => s.Equals(ticketCategory, StringComparison.OrdinalIgnoreCase)) == true ? 5.0 : 0;
        }
        catch
        {
            return 0;
        }
    }
}
```

---

## Benchmarks

### Test Scenario

- 50 active employees
- 100 tickets in various states
- 20ms average query latency
- SQLite with FTS5

### Expected Results

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| SQL Queries | 103 | 3 | **97% reduction** |
| Total Time | 2,060ms | ~80ms | **96% faster** |
| Allocations | High (per-query) | Low (batched) | **~80% reduction** |
| CPU Usage | Low (IO-bound) | Higher (parallel) | Acceptable |

### Load Testing

```csharp
[Fact]
public async Task Dispatch_50Agents_Under100ms()
{
    var stopwatch = Stopwatch.StartNew();
    var results = await _strategy.GetRecommendedAgentsAsync(_testTicket, 3);
    stopwatch.Stop();
    
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    results.Should().HaveCount(3);
}

[Fact]
public async Task Dispatch_QueryCount_LessThan5()
{
    var queryCounter = new QueryCounter(_context);
    
    await _strategy.GetRecommendedAgentsAsync(_testTicket, 3);
    
    queryCounter.QueryCount.Should().BeLessThanOrEqualTo(5);
}
```

---

## Migration Plan

### Phase 1: Add AsNoTracking (1 day)
- Add `.AsNoTracking()` to all read-only queries in dispatching
- No functional change, minor improvement

### Phase 2: Extract Data Loading (2 days)
- Create `LoadDispatchingDataAsync()` method
- Move customer load outside loop
- Batch workload calculation

### Phase 3: Parallel Scoring (2 days)
- Implement `Parallel.ForEach` for agent scoring
- Ensure thread-safety of ML prediction pool
- Add benchmarks

### Phase 4: Remove FTS5 N+1 (2 days)
- Replace per-agent FTS with string matching
- Or implement batched FTS query
- Validate accuracy remains acceptable

### Phase 5: Add Caching (1 day)
- Implement `CachedDispatchingService` decorator
- Configure cache duration
- Add cache invalidation on ticket update

### Total: ~8 days

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Parallel.ForEach thread issues | Low | ML.NET pool is thread-safe; test thoroughly |
| FTS string matching less accurate | Medium | A/B test; keep FTS as config option |
| Cache stale data | Low | 5-min TTL; invalidate on assignment |
| Memory pressure from parallel | Low | Limited by agent count (typically <100) |

---

## Rollback Plan

```csharp
// Feature flag for safety
public class DispatchingStrategySelector : IDispatchingStrategySelector
{
    private readonly IConfiguration _config;
    
    public string GetStrategyName(Ticket ticket)
    {
        // Can instantly revert to old strategy
        return _config.GetValue<bool>("GerdaAI:UseOptimizedDispatching") 
            ? "OptimizedDispatching" 
            : "MatrixFactorization";
    }
}
```

---

## Success Metrics

- [ ] Query count ≤ 5 for 50-agent dispatch
- [ ] P95 latency < 100ms for dispatching
- [ ] Zero functional regression (same recommendations)
- [ ] Memory allocations reduced by 50%+
- [ ] All existing tests pass

---

## Related Work

- Depends on: RFC #1 (Dispatching Consolidation) - should implement AFTER consolidation
- Enables: Real-time dispatch suggestions on ticket creation

---

*Ready for implementation. High impact, low risk with proper testing.*
