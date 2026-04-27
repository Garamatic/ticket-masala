# GERDA Deepening Implementation

Implement the deepened GERDA module per RFC #12.

## Architecture
- Deep module: single `IGerda` interface hiding 7+ services and 15+ strategies
- Simple path: `await _gerda.ProcessAsync(guid)` for 90% case
- Advanced path: `Configure().Stages(...).OnProgress(...).ExecuteAsync()` for debugging
- All dependencies (ITicketRepository, ML.NET, config) hidden internally

## Implementation Checklist

### Phase 1: Core Interface & Types
- [ ] Create `IGerda` interface with `ProcessAsync` and `Configure()`
- [ ] Create `GerdaOutcome` record (immutable result)
- [ ] Create `GerdaStage` enum for stage selection
- [ ] Create `IGerdaAdvancedBuilder` fluent interface

### Phase 2: Internal Engine Structure
- [ ] Create internal `GerdaEngine` class (implements IGerda)
- [ ] Create internal stage runner (replaces pipeline)
- [ ] Create strategy registry (name → instance mapping)
- [ ] Migrate existing stage logic to internal engines

### Phase 3: DI Registration
- [ ] Create `AddGerda()` extension method
- [ ] Implement options pattern for configuration
- [ ] Set up in-memory test substitutes

### Phase 4: Migration
- [ ] Update `TicketOrchestrator.CreateTicketAsync()`
- [ ] Update `GerdaBackgroundService`
- [ ] Delete old shallow services

### Phase 5: Testing
- [ ] Create boundary tests
- [ ] Delete redundant unit tests

## Progress Notes
- Worktree: `../ticket-masala-gerda-deepening`
- RFC: https://github.com/Garamatic/ticket-masala/issues/12