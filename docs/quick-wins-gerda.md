# GERDA Quick Wins - Immediate Optimizations

**Status**: Ready to implement  
**Effort**: 1-2 days  
**Impact**: 30-40% performance improvement  
**Risk**: Very Low (no architectural changes)

---

## Overview

While the major architectural improvements in RFCs #7, #8, and #9 require planning and coordination, these **quick wins** can be implemented immediately with minimal risk.

---

## Quick Win #1: Add AsNoTracking() to Read-Only Queries

### File: `MatrixFactorizationDispatchingStrategy.cs`

**Current** (~10 locations):
```csharp
var employees = await _context.Users.OfType<Employee>().ToListAsync();

var customer = await _context.Users.FindAsync(ticket.CreatorGuid.ToString());

var agentWorkloads = await _context.Tickets
    .Where(t => t.ResponsibleId != null)
    .GroupBy(t => t.ResponsibleId)
    .Select(g => new { AgentId = g.Key!, Count = g.Count() })
    .ToDictionaryAsync(x => x.AgentId!, x => x.Count);
```

**Optimized**:
```csharp
var employees = await _context.Users
    .AsNoTracking()  // ✅ Add this
    .OfType<Employee>()
    .ToListAsync();

var customer = await _context.Users
    .AsNoTracking()  // ✅ Add this
    .FirstOrDefaultAsync(u => u.Id == ticket.CreatorGuid.ToString());

var agentWorkloads = await _context.Tickets
    .AsNoTracking()  // ✅ Add this
    .Where(t => t.ResponsibleId != null)
    .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
    .GroupBy(t => t.ResponsibleId)
    .Select(g => new { AgentId = g.Key!, Count = g.Count() })
    .ToDictionaryAsync(x => x.AgentId!, x => x.Count);
```

**Impact**: ~10-15% CPU reduction, fewer allocations

---

## Quick Win #2: Move Customer Lookup Outside Loop

### File: `MatrixFactorizationDispatchingStrategy.cs` (~line 140)

**Current**:
```csharp
foreach (var employee in employees)
{
    // ❌ Query executed for EVERY employee (N times!)
    var customer = await _context.Users.FindAsync(ticket.CreatorGuid.ToString());
    
    var languageScore = AffinityScoring.CalculateLanguageScore(employee, customer);
    var geoScore = AffinityScoring.CalculateGeographyScore(employee, customer);
}
```

**Optimized**:
```csharp
// ✅ Load ONCE before loop
var customer = await _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Id == ticket.CreatorGuid.ToString());

foreach (var employee in employees)
{
    // Use cached reference
    var languageScore = AffinityScoring.CalculateLanguageScore(employee, customer);
    var geoScore = AffinityScoring.CalculateGeographyScore(employee, customer);
}
```

**Impact**: -1 query per agent (50 agents = 49 fewer queries!)

---

## Quick Win #3: Cache FTS5 Results or Skip for Loop

### File: `MatrixFactorizationDispatchingStrategy.cs` (~lines 180-210)

**Current**:
```csharp
foreach (var employee in employees)
{
    // ❌ Raw SQL query per employee
    var specs = JsonSerializer.Deserialize<List<string>>(employee.Specializations);
    var matchQuery = string.Join(" OR ", specs.Select(s => $"\"{s}\""));
    
    var ranks = await _context.Database.SqlQueryRaw<double>(
        "SELECT rank FROM Tickets_Search WHERE rowid = {0} AND Tickets_Search MATCH {1}",
        ticketRowId, matchQuery).ToListAsync();
}
```

**Optimized (Option A - Pre-compute once)**:
```csharp
// ✅ Pre-compute ticket category once
var ticketCategory = AffinityScoring.ExtractCategoryFromTicket(ticket);

foreach (var employee in employees)
{
    // Simple string comparison instead of FTS per agent
    var specs = JsonSerializer.Deserialize<List<string>>(employee.Specializations);
    var ftsScore = specs?.Any(s => s.Equals(ticketCategory, StringComparison.OrdinalIgnoreCase)) == true 
        ? 1.0 
        : 0.0;
}
```

**Optimized (Option B - Cache if must use FTS)**:
```csharp
// ✅ Single FTS query for ticket, cache result
var ticketKeywords = ExtractKeywords(ticket);

// Single query (outside loop)
var allMatches = await _context.Database.SqlQueryRaw<string>(
    "SELECT DISTINCT specializations FROM Employees WHERE ...").ToListAsync();

foreach (var employee in employees)
{
    // In-memory lookup only
    var ftsScore = allMatches.Contains(employee.Specializations) ? 1.0 : 0.0;
}
```

**Impact**: -1 FTS query per agent (expensive!)

---

## Quick Win #4: Add Database Index

### Migration File (New)

```csharp
// Migrations/20250427_AddDispatchingIndexes.cs
public partial class AddDispatchingIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Index for workload calculation (heavily used in dispatching)
        migrationBuilder.CreateIndex(
            name: "IX_Tickets_ResponsibleId_Status_Workload",
            table: "Tickets",
            columns: new[] { "ResponsibleId", "TicketStatus" });

        // Index for customer lookups
        migrationBuilder.CreateIndex(
            name: "IX_Tickets_CreatorGuid",
            table: "Tickets",
            column: "CreatorGuid");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Tickets_ResponsibleId_Status_Workload",
            table: "Tickets");

        migrationBuilder.DropIndex(
            name: "IX_Tickets_CreatorGuid",
            table: "Tickets");
    }
}
```

**Impact**: Faster workload aggregation (from O(n) scan to O(log n) seek)

---

## Quick Win #5: Parallelize Independent Workloads

### File: `MatrixFactorizationDispatchingStrategy.cs`

**Current**:
```csharp
// Sequential queries
var employees = await _context.Users.OfType<Employee>().ToListAsync();
var customer = await _context.Users.FindAsync(ticket.CreatorGuid.ToString());
var agentWorkloads = await _context.Tickets
    .GroupBy(t => t.ResponsibleId)
    .Select(g => new { AgentId = g.Key!, Count = g.Count() })
    .ToDictionaryAsync(x => x.AgentId!, x => x.Count);
```

**Optimized**:
```csharp
// ✅ Parallel independent queries
var employeesTask = _context.Users
    .AsNoTracking()
    .OfType<Employee>()
    .ToListAsync();

var customerTask = _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Id == ticket.CreatorGuid.ToString());

var workloadsTask = _context.Tickets
    .AsNoTracking()
    .Where(t => t.ResponsibleId != null)
    .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
    .GroupBy(t => t.ResponsibleId)
    .Select(g => new { AgentId = g.Key!, Count = g.Count() })
    .ToDictionaryAsync(x => x.AgentId!, x => x.Count);

await Task.WhenAll(employeesTask, customerTask, workloadsTask);

var employees = await employeesTask;
var customer = await customerTask;
var agentWorkloads = await workloadsTask;
```

**Impact**: Query latency reduced from sum to max (3 × 20ms → 20ms)

---

## Quick Win #6: Remove Unused Code

### File: `DispatchingService.cs` (~lines 200-230)

**Dead code to remove**:
```csharp
// ❌ This method is NEVER called - remove it
private DispatchResult GetRecommendedAgentByEngine(Ticket ticket, List<Agent> availableAgents)
{
    var workItem = new TicketWorkItemAdapter(ticket);
    var result = _agentMatchingEngine.RecommendAgent(workItem, availableAgents);
    // ...
}
```

**Also check for**: Unused using statements, commented code, debug Console.WriteLine

**Impact**: Cleaner codebase, less confusion

---

## Implementation Checklist

### Day 1 (2-3 hours)
- [ ] Add `AsNoTracking()` to all dispatching queries
- [ ] Move customer lookup outside employee loop
- [ ] Test locally with 50-agent dataset

### Day 2 (2-3 hours)
- [ ] Generate EF Core migration for indexes
- [ ] Apply migration to test database
- [ ] Benchmark before/after

### Day 3 (2 hours)
- [ ] Parallelize independent queries with `Task.WhenAll`
- [ ] Remove dead code (`GetRecommendedAgentByEngine`)
- [ ] Code review and merge

---

## Testing

### Before/After Benchmark

```csharp
[Fact]
public async Task DispatchPerformance_Benchmark()
{
    var stopwatch = Stopwatch.StartNew();
    var queryCounter = new QueryCounter(_context);
    
    var results = await _strategy.GetRecommendedAgentsAsync(_testTicket, 3);
    
    stopwatch.Stop();
    
    _output.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
    _output.WriteLine($"Queries: {queryCounter.QueryCount}");
    
    // Assert improvements
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Before: ~2000ms
    queryCounter.QueryCount.Should().BeLessThan(20);       // Before: ~103
}
```

### Load Test

```bash
# Use k6 or similar to simulate dispatch load
# Before optimization: RPS ~5, latency ~2000ms
# After optimization: RPS ~50, latency ~100ms
```

---

## Expected Results

| Metric | Before | After Quick Wins | Improvement |
|--------|--------|------------------|-------------|
| SQL Queries (50 agents) | ~103 | ~55 | 47% |
| Total Time | ~2s | ~500ms | 75% |
| Memory Allocations | High | Medium | ~40% |
| Code Clarity | Confusing | Better | N/A |

---

## Rollback Plan

All changes are additive or simple refactors:
- Remove `AsNoTracking()` if issues arise
- Move customer back into loop (1 line change)
- Drop indexes if they hurt write performance
- Revert parallel queries to sequential

**Risk Level**: Very Low

---

## Next Steps

After quick wins are deployed and stable, proceed with:

1. [RFC #7 - Dispatching Consolidation](https://github.com/Garamatic/ticket-masala/issues/7)
2. [RFC #8 - Pipeline Error Handling](https://github.com/Garamatic/ticket-masala/issues/8)
3. [RFC #9 - Query Optimization](https://github.com/Garamatic/ticket-masala/issues/9)

---

*Implement these now for immediate gains while planning major improvements.*
