# RFC: Pipeline Error Handling & Result Pattern

**Status**: Proposed  
**Author**: AI Architecture Review  
**Date**: 2025-04-27  
**Related**: `ConfigurableGerdaPipeline.cs`, `IGerdaStage.cs`

---

## Summary

The current GERDA pipeline silently swallows stage failures, making it impossible for callers to know if processing partially failed. This RFC proposes a `PipelineResult` pattern with per-stage status reporting, supporting both "continue on error" and "fail fast" execution modes.

---

## Current Problem

### Silent Failures

```csharp
// ConfigurableGerdaPipeline.cs (current)
public async Task<GerdaPipelineContext> ExecuteAsync(Guid ticketGuid)
{
    foreach (var stage in _stages)
    {
        try
        {
            await stage.ExecuteAsync(ticketGuid, context);
        }
        catch (Exception ex)
        {
            // ERROR IS LOGGED BUT SWALLOWED!
            _logger.LogError(ex, "Stage {StageName} failed...");
            // No way for caller to know this happened
        }
    }
    return context; // Looks like success
}
```

### Impact

```csharp
// Caller has no idea grouping failed
var context = await _pipeline.ExecuteAsync(ticketGuid);
// context.ParentTicketGuid is null, but was it because:
// A) No grouping needed? or B) Grouping stage crashed?

if (context.RecommendedAgentId == null)
{
    // Was dispatching disabled, or did it fail?
    // No way to tell!
}
```

---

## Proposed Design

### PipelineResult Pattern

```csharp
// New file: Engine/GERDA/Pipeline/PipelineResult.cs
public class PipelineResult
{
    public Guid TicketGuid { get; init; }
    public bool IsSuccess => !StageResults.Any(s => s.IsFailure && s.IsCritical);
    public bool HasPartialFailure => StageResults.Any(s => s.IsFailure && !s.IsCritical);
    public IReadOnlyList<StageResult> StageResults { get; init; } = new List<StageResult>();
    public GerdaPipelineContext Context { get; init; } = new();
    public TimeSpan TotalDuration { get; init; }
    
    public StageResult? GetStageResult(string stageName) => 
        StageResults.FirstOrDefault(s => s.StageName == stageName);
}

public class StageResult
{
    public string StageName { get; init; } = string.Empty;
    public StageStatus Status { get; init; }
    public bool IsSuccess => Status == StageStatus.Completed;
    public bool IsFailure => Status == StageStatus.Failed || Status == StageStatus.TimedOut;
    public bool IsSkipped => Status == StageStatus.Skipped || Status == StageStatus.Disabled;
    public bool IsCritical { get; init; }  // True = fail pipeline on error
    public Exception? Error { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
}

public enum StageStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    TimedOut,
    Skipped,    // Dependency not met (e.g., previous stage failed)
    Disabled    // Stage.IsEnabled == false
}
```

### Enhanced IGerdaStage

```csharp
// Updated: Engine/GERDA/Pipeline/IGerdaStage.cs
public interface IGerdaStage
{
    string StageName { get; }
    bool IsEnabled { get; }
    bool ContinueOnError { get; }  // NEW: Stage decides if pipeline continues
    int TimeoutMs { get; }          // NEW: Per-stage timeout (default: 30000)
    
    Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context);
}

// Base implementation for convenience
public abstract class GerdaStageBase : IGerdaStage
{
    public abstract string StageName { get; }
    public virtual bool IsEnabled => true;
    public virtual bool ContinueOnError => true;  // Default: continue
    public virtual int TimeoutMs => 30000;        // Default: 30 seconds
    
    public abstract Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context);
}
```

### ConfigurableGerdaPipeline V2

```csharp
public class ConfigurableGerdaPipeline : IGerdaPipeline
{
    private readonly List<IGerdaStage> _stages;
    private readonly ILogger<ConfigurableGerdaPipeline> _logger;
    private readonly PipelineOptions _options;

    public async Task<PipelineResult> ExecuteAsync(Guid ticketGuid)
    {
        var stopwatch = Stopwatch.StartNew();
        var context = new GerdaPipelineContext();
        var stageResults = new List<StageResult>();
        var hasCriticalFailure = false;

        _logger.LogInformation(
            "GERDA Pipeline: Starting {TicketGuid} through {Count} stages",
            ticketGuid, _stages.Count);

        foreach (var stage in _stages.Where(s => s.IsEnabled))
        {
            if (hasCriticalFailure)
            {
                stageResults.Add(CreateSkippedResult(stage, "Previous critical stage failed"));
                continue;
            }

            var stageResult = await ExecuteStageWithTimeoutAsync(stage, ticketGuid, context);
            stageResults.Add(stageResult);

            if (stageResult.IsFailure)
            {
                if (!stage.ContinueOnError)
                {
                    hasCriticalFailure = true;
                    _logger.LogError(
                        "GERDA Pipeline: Critical stage {StageName} failed. Aborting.",
                        stage.StageName);
                    
                    if (_options.FailFast)
                        break;
                }
            }
        }

        stopwatch.Stop();
        
        var result = new PipelineResult
        {
            TicketGuid = ticketGuid,
            StageResults = stageResults.AsReadOnly(),
            Context = context,
            TotalDuration = stopwatch.Elapsed
        };

        LogPipelineCompletion(result);
        return result;
    }

    private async Task<StageResult> ExecuteStageWithTimeoutAsync(
        IGerdaStage stage, 
        Guid ticketGuid, 
        GerdaPipelineContext context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        using var cts = new CancellationTokenSource(stage.TimeoutMs);
        
        try
        {
            _logger.LogDebug("GERDA Pipeline: Starting stage {StageName}", stage.StageName);
            
            await stage.ExecuteAsync(ticketGuid, context)
                      .WaitAsync(cts.Token);
            
            stopwatch.Stop();
            
            return new StageResult
            {
                StageName = stage.StageName,
                Status = StageStatus.Completed,
                IsCritical = !stage.ContinueOnError,
                Duration = stopwatch.Elapsed,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            stopwatch.Stop();
            
            return new StageResult
            {
                StageName = stage.StageName,
                Status = StageStatus.TimedOut,
                IsCritical = !stage.ContinueOnError,
                ErrorMessage = $"Stage timed out after {stage.TimeoutMs}ms",
                Duration = stopwatch.Elapsed,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            return new StageResult
            {
                StageName = stage.StageName,
                Status = StageStatus.Failed,
                IsCritical = !stage.ContinueOnError,
                Error = ex,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
    }

    private void LogPipelineCompletion(PipelineResult result)
    {
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "GERDA Pipeline: Completed {TicketGuid} successfully in {DurationMs}ms",
                result.TicketGuid, result.TotalDuration.TotalMilliseconds);
        }
        else if (result.HasPartialFailure)
        {
            _logger.LogWarning(
                "GERDA Pipeline: Completed {TicketGuid} with partial failures in {DurationMs}ms. " +
                "Failed stages: {FailedStages}",
                result.TicketGuid,
                result.TotalDuration.TotalMilliseconds,
                string.Join(", ", result.StageResults.Where(s => s.IsFailure).Select(s => s.StageName)));
        }
        else
        {
            _logger.LogError(
                "GERDA Pipeline: Failed {TicketGuid} with critical error in {DurationMs}ms",
                result.TicketGuid, result.TotalDuration.TotalMilliseconds);
        }
    }
}
```

---

## Stage Configuration

### Declarative Error Handling

```csharp
// Example: DispatchingStage with custom error handling
public class DispatchingStage : GerdaStageBase
{
    private readonly IDispatchingService? _dispatchingService;
    private readonly ILogger<DispatchingStage> _logger;

    public override string StageName => "Dispatching";
    
    // Dispatching failure should not block other stages
    public override bool ContinueOnError => true;
    
    // But should complete within 10 seconds
    public override int TimeoutMs => 10000;

    public override async Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context)
    {
        if (_dispatchingService?.IsEnabled != true)
            return;

        var recommendedAgent = await _dispatchingService.GetRecommendedAgentAsync(ticketGuid);
        
        if (!string.IsNullOrEmpty(recommendedAgent) && Guid.TryParse(recommendedAgent, out var agentGuid))
        {
            context.RecommendedAgentId = agentGuid;
        }
    }
}

// Example: GroupingStage where failure matters
public class GroupingStage : GerdaStageBase
{
    public override string StageName => "Grouping";
    
    // Grouping failure should stop pipeline (child tickets need parent!)
    public override bool ContinueOnError => false;
    
    public override int TimeoutMs => 5000;

    public override async Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context)
    {
        // If this throws, pipeline stops
        var parentGuid = await _groupingService.CheckAndGroupTicketAsync(ticketGuid);
        if (parentGuid.HasValue)
        {
            context.ParentTicketGuid = parentGuid.Value;
        }
    }
}
```

---

## Usage Patterns

### Pattern 1: Fail Fast (Default)

```csharp
// Stop on first critical failure
var result = await _pipeline.ExecuteAsync(ticketGuid);

if (!result.IsSuccess)
{
    var failedStage = result.StageResults.First(s => s.IsFailure && s.IsCritical);
    _logger.LogError("Pipeline stopped at {Stage}: {Error}", 
        failedStage.StageName, 
        failedStage.ErrorMessage);
    
    // Notify admin, don't process ticket further
    await _alertService.SendPipelineFailureAlert(result);
    return;
}

// Continue with successful processing
await SavePipelineResults(result.Context);
```

### Pattern 2: Best Effort (Continue on Error)

```csharp
// Process what we can, report partial success
var result = await _pipeline.ExecuteAsync(ticketGuid);

if (result.HasPartialFailure)
{
    foreach (var failed in result.StageResults.Where(s => s.IsFailure))
    {
        _metrics.RecordStageFailure(failed.StageName, failed.Error?.GetType().Name);
    }
}

// Always save what we got (partial enrichment is better than none)
await SavePipelineResults(result.Context);
```

### Pattern 3: Stage-Specific Recovery

```csharp
var result = await _pipeline.ExecuteAsync(ticketGuid);

// Check specific stage results
var dispatchingResult = result.GetStageResult("Dispatching");
if (dispatchingResult?.IsFailure == true)
{
    // Fallback to round-robin assignment
    await _fallbackDispatchService.AssignRoundRobin(ticketGuid);
}

var rankingResult = result.GetStageResult("Ranking");
if (rankingResult?.IsFailure == true)
{
    // Use default priority
    await _ticketService.SetDefaultPriority(ticketGuid);
}
```

---

## Dependency-Aware Execution

### Stage Dependencies

```csharp
// New interface for stages that depend on others
public interface IGerdaStageWithDependencies : IGerdaStage
{
    IReadOnlyList<string> DependsOn { get; }
    bool CanExecuteWithoutDependencies { get; }
}

// Example: Dispatching depends on Ranking score
public class DispatchingStage : GerdaStageBase, IGerdaStageWithDependencies
{
    public override string StageName => "Dispatching";
    
    public IReadOnlyList<string> DependsOn => new[] { "Ranking" };
    
    public bool CanExecuteWithoutDependencies => true; // Can dispatch without ranking
    
    public override async Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context)
    {
        // Can check if dependency produced result
        if (context.PriorityScore.HasValue)
        {
            // Use priority in dispatching decision
            _logger.LogDebug("Dispatching with priority {Score}", context.PriorityScore);
        }
        
        // Continue regardless
        await base.ExecuteAsync(ticketGuid, context);
    }
}
```

---

## Monitoring & Observability

### Metrics

```csharp
public interface IPipelineMetrics
{
    void RecordPipelineExecution(TimeSpan duration, bool success);
    void RecordStageExecution(string stageName, TimeSpan duration, StageStatus status);
    void RecordStageTimeout(string stageName);
    void RecordStageFailure(string stageName, string exceptionType);
}

// Prometheus integration
public class PrometheusPipelineMetrics : IPipelineMetrics
{
    private readonly Counter _pipelineCounter;
    private readonly Histogram _pipelineDuration;
    private readonly Counter _stageCounter;
    
    public void RecordStageExecution(string stageName, TimeSpan duration, StageStatus status)
    {
        _stageCounter.WithLabels(stageName, status.ToString()).Inc();
    }
}
```

### Distributed Tracing

```csharp
public async Task<PipelineResult> ExecuteAsync(Guid ticketGuid)
{
    using var activity = new Activity("GERDA.Pipeline").Start();
    activity.SetTag("ticket.guid", ticketGuid);
    
    foreach (var stage in _stages)
    {
        using var stageActivity = new Activity($"GERDA.Stage.{stage.StageName}").Start();
        
        var result = await ExecuteStageAsync(stage, ticketGuid, context);
        
        stageActivity.SetTag("stage.success", result.IsSuccess);
        stageActivity.SetTag("stage.duration_ms", result.Duration.TotalMilliseconds);
        
        if (result.IsFailure)
        {
            stageActivity.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
        }
    }
}
```

---

## Migration Path

### Phase 1: Add Result Type (Backward Compatible)

```csharp
// Keep existing interface, add new one
public interface IGerdaPipelineV2 : IGerdaPipeline
{
    new Task<PipelineResult> ExecuteAsync(Guid ticketGuid);
}

// Implement both
public class ConfigurableGerdaPipeline : IGerdaPipeline, IGerdaPipelineV2
{
    // Old method delegates to new
    async Task<GerdaPipelineContext> IGerdaPipeline.ExecuteAsync(Guid ticketGuid)
    {
        var result = await ExecuteAsync(ticketGuid);
        return result.Context;
    }
    
    // New method returns full result
    public async Task<PipelineResult> ExecuteAsync(Guid ticketGuid)
    {
        // ... new implementation
    }
}
```

### Phase 2: Update Callers Gradually

```csharp
// Before
var context = await _pipeline.ExecuteAsync(ticketGuid);

// After (gradual migration)
var result = await ((IGerdaPipelineV2)_pipeline).ExecuteAsync(ticketGuid);
if (!result.IsSuccess) { /* handle */ }
```

### Phase 3: Remove Legacy Interface

In v2.0, remove `IGerdaPipeline` (context-only) and keep `IGerdaPipelineV2` → rename to `IGerdaPipeline`.

---

## Testing Strategy

### Unit Tests

```csharp
public class ConfigurableGerdaPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_WithAllStagesPassing_ReturnsSuccess()
    {
        var stages = new[] { CreatePassingStage("A"), CreatePassingStage("B") };
        var pipeline = new ConfigurableGerdaPipeline(stages, _logger, _options);
        
        var result = await pipeline.ExecuteAsync(Guid.NewGuid());
        
        result.IsSuccess.Should().BeTrue();
        result.StageResults.Should().AllSatisfy(s => s.IsSuccess.Should().BeTrue());
    }
    
    [Fact]
    public async Task ExecuteAsync_WithContinueOnError_FailureDoesNotStopPipeline()
    {
        var stages = new[] 
        { 
            CreateFailingStage("A", continueOnError: true),
            CreatePassingStage("B")
        };
        var pipeline = new ConfigurableGerdaPipeline(stages, _logger, _options);
        
        var result = await pipeline.ExecuteAsync(Guid.NewGuid());
        
        result.IsSuccess.Should().BeTrue(); // Not critical failure
        result.HasPartialFailure.Should().BeTrue();
        result.StageResults[1].IsSuccess.Should().BeTrue(); // B still ran
    }
    
    [Fact]
    public async Task ExecuteAsync_WithFailFast_FailureStopsPipeline()
    {
        var stageB = CreatePassingStage("B");
        var stages = new[] 
        { 
            CreateFailingStage("A", continueOnError: false),  // Critical
            stageB
        };
        var pipeline = new ConfigurableGerdaPipeline(stages, _logger, _options);
        
        var result = await pipeline.ExecuteAsync(Guid.NewGuid());
        
        result.IsSuccess.Should().BeFalse();
        stageB.WasExecuted.Should().BeFalse(); // Never got to B
    }
    
    [Fact]
    public async Task ExecuteAsync_WithTimeout_ReportsTimeoutStatus()
    {
        var slowStage = CreateSlowStage("Slow", delay: TimeSpan.FromSeconds(10), timeoutMs: 100);
        var pipeline = new ConfigurableGerdaPipeline(new[] { slowStage }, _logger, _options);
        
        var result = await pipeline.ExecuteAsync(Guid.NewGuid());
        
        result.StageResults[0].Status.Should().Be(StageStatus.TimedOut);
    }
}
```

---

## Implementation Checklist

- [ ] Create `PipelineResult`, `StageResult`, `StageStatus` types
- [ ] Add `ContinueOnError` and `TimeoutMs` to `IGerdaStage`
- [ ] Implement `ConfigurableGerdaPipelineV2` with result pattern
- [ ] Add per-stage timeout handling with `CancellationTokenSource`
- [ ] Implement dependency-aware execution (optional)
- [ ] Add `IPipelineMetrics` interface and Prometheus implementation
- [ ] Update all existing stages to inherit `GerdaStageBase`
- [ ] Add tests for success, failure, timeout, skip scenarios
- [ ] Create migration guide for callers
- [ ] Update documentation with new patterns

---

*Ready for review. This is a breaking change requiring major version bump.*
