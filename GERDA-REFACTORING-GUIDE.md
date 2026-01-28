# GERDA Service Refactoring - Migration Guide

## Overview

The GERDA service has been refactored from a **god object** into a clean **Pipeline pattern** implementation. This improves:

- **Single Responsibility Principle**: Each stage has one clear purpose
- **Open/Closed Principle**: Add new stages without modifying existing code
- **Testability**: Test each stage independently
- **Maintainability**: Easier to understand and debug

## Architecture Change

### Before (God Object)

```
GerdaService (180 lines)
├── ProcessTicketAsync()
│   ├── Direct calls to 6+ services
│   ├── Mixed orchestration + error handling
│   └── Conditional logic for optional services
```

### After (Pipeline Pattern)

```
GerdaServiceV2 (Thin orchestrator)
├── Delegates to IGerdaPipeline
│   └── ConfigurableGerdaPipeline
│       ├── GroupingStage (G)
│       ├── EstimatingStage (E)
│       ├── RankingStage (R)
│       ├── DispatchingStage (D)
│       └── KnowledgeStage (K)
```

## Migration Steps

### Step 1: Update DI Registration

In `WebApplicationBuilderExtensions.cs` or your GERDA configuration file:

```csharp
// OLD (God object approach)
builder.Services.AddScoped<IGerdaService, GerdaService>();

// NEW (Pipeline pattern approach)
// Register individual stages
builder.Services.AddScoped<IGerdaStage, GroupingStage>();
builder.Services.AddScoped<IGerdaStage, EstimatingStage>();
builder.Services.AddScoped<IGerdaStage, RankingStage>();
builder.Services.AddScoped<IGerdaStage, DispatchingStage>();
builder.Services.AddScoped<IGerdaStage, KnowledgeStage>();

// Register pipeline
builder.Services.AddScoped<IGerdaPipeline, ConfigurableGerdaPipeline>();

// Register service (uses pipeline)
builder.Services.AddScoped<IGerdaService, GerdaServiceV2>();
```

### Step 2: No Code Changes Required

The `IGerdaService` interface remains the same, so **no changes needed** in:
- Controllers
- Background jobs
- Tests (unless you want to test individual stages)

### Step 3: Optional - Add Custom Stages

To add a new GERDA stage (e.g., Sentiment Analysis):

```csharp
public class SentimentAnalysisStage : IGerdaStage
{
    private readonly ISentimentService _sentimentService;
    private readonly ILogger<SentimentAnalysisStage> _logger;

    public SentimentAnalysisStage(
        ISentimentService sentimentService,
        ILogger<SentimentAnalysisStage> logger)
    {
        _sentimentService = sentimentService;
        _logger = logger;
    }

    public string StageName => "SentimentAnalysis";
    public bool IsEnabled => true; // Or read from config

    public async Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context)
    {
        var sentiment = await _sentimentService.AnalyzeAsync(ticketGuid);
        context.Set("sentiment", sentiment);
        
        _logger.LogInformation(
            "GERDA-S: Ticket {TicketGuid} sentiment: {Sentiment}",
            ticketGuid, sentiment);
    }
}

// Register it
builder.Services.AddScoped<IGerdaStage, SentimentAnalysisStage>();
```

The pipeline automatically picks it up. **No changes to GerdaServiceV2 required.**

## Benefits

### 1. Testability

**Before:**
```csharp
// Had to mock 6+ services to test GerdaService
var mockGrouping = new Mock<IGroupingService>();
var mockEstimating = new Mock<IEstimatingService>();
var mockRanking = new Mock<IRankingService>();
// ... 3 more mocks

var service = new GerdaService(/* 8 dependencies */);
```

**After:**
```csharp
// Test individual stages with 1-2 dependencies
var mockGrouping = new Mock<IGroupingService>();
var stage = new GroupingStage(mockGrouping.Object, logger);
var context = new GerdaPipelineContext();
await stage.ExecuteAsync(ticketGuid, context);

Assert.NotNull(context.ParentTicketGuid);
```

### 2. Single Responsibility

Each stage has **one job**:
- `GroupingStage`: Check for duplicates/spam
- `EstimatingStage`: Calculate effort
- `RankingStage`: Calculate priority
- `DispatchingStage`: Recommend agent
- `KnowledgeStage`: Suggest articles

### 3. Open/Closed Principle

Add new stages without modifying existing code:

```csharp
// Add Translation stage - no changes to pipeline or other stages
builder.Services.AddScoped<IGerdaStage, TranslationStage>();
```

### 4. Error Isolation

If one stage fails, the pipeline continues with the next stage:

```
GERDA-G: ✅ Grouping completed
GERDA-E: ✅ Estimating completed
GERDA-R: ❌ Ranking failed (API timeout)
GERDA-D: ✅ Dispatching completed (uses previous context)
GERDA-K: ✅ Knowledge completed
```

## Backward Compatibility

### Option 1: Keep Both (Recommended for gradual migration)

```csharp
// Register both implementations
builder.Services.AddScoped<GerdaService>(); // Old implementation
builder.Services.AddScoped<GerdaServiceV2>(); // New implementation
builder.Services.AddScoped<IGerdaService>(sp => 
{
    // Feature flag to switch between implementations
    var useV2 = sp.GetRequiredService<IConfiguration>()
        .GetValue<bool>("Features:UseGerdaV2");
    
    return useV2 
        ? sp.GetRequiredService<GerdaServiceV2>() 
        : sp.GetRequiredService<GerdaService>();
});
```

### Option 2: Direct Replacement

```csharp
// Simply replace the registration
builder.Services.AddScoped<IGerdaService, GerdaServiceV2>();
```

## Testing Examples

### Test Individual Stage

```csharp
[Fact]
public async Task EstimatingStage_CalculatesEffortPoints()
{
    // Arrange
    var mockEstimating = new Mock<IEstimatingService>();
    mockEstimating.Setup(x => x.EstimateComplexityAsync(It.IsAny<Guid>()))
        .ReturnsAsync(5.0);
    
    var stage = new EstimatingStage(mockEstimating.Object, _logger);
    var context = new GerdaPipelineContext();
    var ticketGuid = Guid.NewGuid();
    
    // Act
    await stage.ExecuteAsync(ticketGuid, context);
    
    // Assert
    Assert.Equal(5.0, context.EffortPoints);
}
```

### Test Pipeline

```csharp
[Fact]
public async Task Pipeline_ExecutesAllEnabledStages()
{
    // Arrange
    var stages = new List<IGerdaStage>
    {
        new GroupingStage(mockGrouping.Object, _logger),
        new EstimatingStage(mockEstimating.Object, _logger)
    };
    
    var pipeline = new ConfigurableGerdaPipeline(stages, _logger);
    
    // Act
    var context = await pipeline.ExecuteAsync(Guid.NewGuid());
    
    // Assert
    Assert.NotNull(context.EffortPoints);
}
```

## Performance Considerations

**No performance degradation expected.** The pipeline executes stages sequentially, just like the old implementation. The only overhead is the `foreach` loop, which is negligible (~nanoseconds).

## Rollback Plan

If issues arise, simply revert the DI registration:

```csharp
// Revert to old implementation
builder.Services.AddScoped<IGerdaService, GerdaService>();
```

No code changes needed elsewhere since the interface remains the same.

---

**Status:** ✅ READY FOR PRODUCTION  
**Breaking Changes:** None (interface unchanged)  
**Recommended Timeline:** 
- Week 1: Deploy with feature flag (both implementations available)
- Week 2: Monitor logs for issues
- Week 3: Switch to V2 by default
- Week 4: Remove old implementation if no issues

