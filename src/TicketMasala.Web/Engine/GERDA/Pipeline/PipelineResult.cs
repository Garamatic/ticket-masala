namespace TicketMasala.Web.Engine.GERDA.Pipeline;

/// <summary>
/// Represents the result of a pipeline execution, including per-stage status.
/// This replaces the silent failure behavior with explicit error reporting (Issue #8).
/// </summary>
public record PipelineResult
{
    /// <summary>
    /// Gets the ticket GUID that was processed.
    /// </summary>
    public Guid TicketGuid { get; init; }

    /// <summary>
    /// Gets the overall success status of the pipeline.
    /// Returns true only if all stages succeeded (or if failures were explicitly allowed).
    /// </summary>
    public bool IsSuccess => _stageResults.All(r => r.Status == StageStatus.Succeeded || r.Status == StageStatus.Skipped);

    /// <summary>
    /// Gets whether any stage failed during execution.
    /// </summary>
    public bool HasFailures => _stageResults.Any(r => r.Status == StageStatus.Failed);

    /// <summary>
    /// Gets the pipeline context containing data from all stages.
    /// </summary>
    public GerdaPipelineContext Context { get; init; } = new();

    /// <summary>
    /// Gets the execution mode used for this pipeline run.
    /// </summary>
    public PipelineExecutionMode ExecutionMode { get; init; } = PipelineExecutionMode.ContinueOnError;

    /// <summary>
    /// Gets the total time taken for pipeline execution.
    /// </summary>
    public TimeSpan Duration { get; init; }

    private readonly List<StageResult> _stageResults = new();

    /// <summary>
    /// Gets the detailed results for each stage.
    /// </summary>
    public IReadOnlyList<StageResult> StageResults => _stageResults.AsReadOnly();

    /// <summary>
    /// Adds a stage result to the pipeline result.
    /// </summary>
    internal void AddStageResult(StageResult result) => _stageResults.Add(result);

    /// <summary>
    /// Gets a summary of the pipeline execution for logging.
    /// </summary>
    public string GetSummary()
    {
        var succeeded = _stageResults.Count(r => r.Status == StageStatus.Succeeded);
        var failed = _stageResults.Count(r => r.Status == StageStatus.Failed);
        var skipped = _stageResults.Count(r => r.Status == StageStatus.Skipped);

        return $"Pipeline for {TicketGuid}: {succeeded} succeeded, {failed} failed, {skipped} skipped in {Duration.TotalMilliseconds:F0}ms";
    }

    /// <summary>
    /// Gets all errors that occurred during pipeline execution.
    /// </summary>
    public IEnumerable<StageError> GetAllErrors() =>
        _stageResults.Where(r => r.Error != null).Select(r => r.Error!);
}

/// <summary>
/// Represents the result of a single stage execution.
/// </summary>
public class StageResult
{
    /// <summary>
    /// Gets the name of the stage.
    /// </summary>
    public required string StageName { get; init; }

    /// <summary>
    /// Gets the execution status of the stage.
    /// </summary>
    public required StageStatus Status { get; init; }

    /// <summary>
    /// Gets the time taken to execute this stage.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the error information if the stage failed.
    /// </summary>
    public StageError? Error { get; init; }

    /// <summary>
    /// Gets optional metadata about the stage execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Represents an error that occurred during stage execution.
/// </summary>
public class StageError
{
    /// <summary>
    /// Gets the name of the stage where the error occurred.
    /// </summary>
    public required string StageName { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the full exception details if available.
    /// </summary>
    public string? ExceptionDetails { get; init; }

    /// <summary>
    /// Gets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Execution status of a pipeline stage.
/// </summary>
public enum StageStatus
{
    /// <summary>
    /// Stage executed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Stage was skipped (e.g., disabled or preconditions not met).
    /// </summary>
    Skipped,

    /// <summary>
    /// Stage failed with an error.
    /// </summary>
    Failed
}

/// <summary>
/// Defines how the pipeline should handle stage failures.
/// </summary>
public enum PipelineExecutionMode
{
    /// <summary>
    /// Continue executing remaining stages even if one fails.
    /// This is the legacy behavior for backward compatibility.
    /// </summary>
    ContinueOnError,

    /// <summary>
    /// Stop pipeline execution immediately on first failure.
    /// </summary>
    FailFast
}

/// <summary>
/// Configuration options for pipeline execution.
/// </summary>
public class PipelineOptions
{
    /// <summary>
    /// Gets or sets the execution mode for handling stage failures.
    /// Default is ContinueOnError for backward compatibility.
    /// </summary>
    public PipelineExecutionMode ExecutionMode { get; set; } = PipelineExecutionMode.ContinueOnError;

    /// <summary>
    /// Gets or sets whether to capture detailed timing information for each stage.
    /// </summary>
    public bool CaptureTiming { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum time allowed for the entire pipeline.
    /// Null means no timeout.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}
