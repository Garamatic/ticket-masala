namespace TicketMasala.Domain.Ports;

/// <summary>
/// Domain port for AI text generation.
/// Implemented by production adapters (OpenAI, OpenRouter, local LLM)
/// and by test doubles.
/// </summary>
public interface IAIGenerationPort
{
    /// <summary>
    /// Generates text for a domain-defined operation.
    /// </summary>
    /// <param name="request">
    /// The generation request carrying an operation tag, content, and optional directive.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A completion result. Never null; on total failure the adapter throws
    /// <see cref="InvalidOperationException"/> or returns a result with an error message
    /// in <see cref="AICompletion.Text"/> depending on adapter policy.
    /// </returns>
    Task<AICompletion> CompleteAsync(
        AICompletionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A domain-level request for AI text generation.
/// Contains no provider-specific concepts (no model names, no system prompts,
/// no temperature, no token limits).
/// </summary>
public sealed record AICompletionRequest
{
    /// <summary>
    /// Domain operation tag. Examples: "summarize", "roadmap", "classify-priority",
    /// "explain-assignment", "generate-title".
    /// The adapter maps this tag to a prompt template and model configuration.
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// The content to be processed (ticket description, project brief, comment thread, etc.).
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Optional domain-level directive. Not a raw system prompt —
    /// a high-level instruction such as "be concise" or "include risks".
    /// The adapter decides how to surface this to the underlying provider.
    /// </summary>
    public string? Directive { get; init; }
}

/// <summary>
/// Result of an AI generation call. Provider-agnostic.
/// </summary>
public sealed record AICompletion
{
    /// <summary>
    /// Generated text. Never null; may be empty on failure depending on adapter policy.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Whether this result was served from cache.
    /// </summary>
    public bool FromCache { get; init; }

    /// <summary>
    /// Opaque diagnostics bag for logging, cost tracking, and observability.
    /// Keys are adapter-defined (e.g., "model", "duration_ms", "provider").
    /// The domain should not depend on specific keys.
    /// </summary>
    public IReadOnlyDictionary<string, object> Diagnostics { get; init; }
        = new Dictionary<string, object>();
}
