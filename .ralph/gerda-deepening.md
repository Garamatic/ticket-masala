# GERDA Deepening Implementation

Implement the deepened GERDA module per RFC #12.

## Architecture
- Deep module: single `IGerda` interface hiding 7+ services and 15+ strategies
- Simple path: `await _gerda.ProcessAsync(guid)` for 90% case
- Advanced path: `Configure().Stages(...).OnProgress(...).ExecuteAsync()` for debugging
- All dependencies (ITicketRepository, ML.NET, config) hidden internally

## Implementation Checklist

### Phase 1: Core Interface & Types ✅ COMPLETE
- [x] Create `IGerda` interface with `ProcessAsync` and `Configure()`
- [x] Create `GerdaOutcome` record (immutable result)
- [x] Create `GerdaStage` enum for stage selection
- [x] Create `IGerdaAdvancedBuilder` fluent interface

### Phase 2: Internal Engine Structure ✅ COMPLETE
- [x] Create internal `GerdaEngine` class (implements IGerda)
- [x] Create internal stage runner (replaces pipeline)
- [x] Create strategy registry (name → instance mapping)
- [x] Migrate existing stage logic to internal engines

### Phase 3: DI Registration ✅ COMPLETE
- [x] Create `AddGerda()` extension method
- [x] Implement options pattern for configuration
- [x] Set up in-memory test substitutes

### Phase 4: Migration ✅ COMPLETE
- [x] Update `TicketOrchestrator.CreateTicketAsync()`
- [x] Replace `GerdaBackgroundService` with `GerdaMaintenanceService`
- [x] Legacy services remain for backward compatibility (migration path documented)

### Phase 5: Testing ✅ COMPLETE
- [x] All 308 tests passing
- [x] 24 tests skipped (pre-existing)
- [x] 0 tests failing

## Summary

**New Files Created:**
- `IGerda.cs` — Public interface with `ProcessAsync()` and `Configure()`
- `GerdaEngine.cs` — Deep module implementation
- `NoOpGerda.cs` — No-op implementation when disabled
- `GerdaMaintenanceService.cs` — Background service using new interface
- `Internal/IStageEngines.cs` — Internal engine interfaces
- `Internal/*Engine.cs` — 6 stage engine wrappers
- `StrategyRegistry.cs` — Name → strategy mapping
- `GerdaServiceExtensions.cs` — `AddGerda()` DI registration

**Key Changes:**
- `Program.cs` — Updated to use `AddGerda()`
- `TicketOrchestrator.cs` — Migrated from `IGerdaService` to `IGerda`
- `WebApplicationBuilderExtensions.cs` — Removed old GERDA registration

**Test Results:**
```
Passed!  - Failed:     0, Passed:   308, Skipped:    24, Total:   332
```

**Worktree:** `../ticket-masala-gerda-deepening`  
**RFC:** https://github.com/Garamatic/ticket-masala/issues/12

---

## ✅ COMPLETE

<promise>COMPLETE</promise>
