# Implement GitHub Issues #7, #8, #9

## Issue #7: Consolidate GERDA Dispatching Implementations
**Priority: 1** (architectural foundation)
- [ ] Read MatrixFactorizationDispatchingStrategy.cs, AgentMatchingEngine.cs, DispatchingService.cs
- [ ] Identify the two competing implementation paths
- [ ] Design Adapter pattern: AgentMatchingEngine as primary, MatrixFactorization as affinity scoring adapter
- [ ] Implement consolidation
- [ ] Run tests to verify behavior preserved

## Issue #9: Optimize GERDA Dispatching Database Queries (N+1 Elimination)
**Priority: 2** (depends on #7 for clean target)
- [ ] Read the consolidated dispatching implementation from #7
- [ ] Identify N+1 queries (customer per agent, FTS5 per agent)
- [ ] Pre-load customer ONCE before loop
- [ ] Batch/pre-load agent specializations and FTS5 matches
- [ ] Add query optimization with caching
- [ ] Run tests and verify query reduction

## Issue #8: Pipeline Error Handling & Result Pattern
**Priority: 3** (independent, GERDA pipeline)
- [ ] Read ConfigurableGerdaPipeline.cs, IGerdaStage.cs
- [ ] Design PipelineResult class with per-stage status
- [ ] Implement "continue on error" and "fail fast" modes
- [ ] Add configuration option
- [ ] Run tests for both modes

## Completion Criteria
- All 178 existing tests pass
- New tests added for #8 and #9
- Code compiles without warnings
- Pre-commit hooks pass