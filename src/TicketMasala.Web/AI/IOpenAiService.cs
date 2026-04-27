namespace TicketMasala.Web.AI;

/// <summary>
/// Service for interacting with OpenAI-compatible APIs.
/// Provides text generation with support for multiple providers (OpenAI, OpenRouter).
/// </summary>
public interface IOpenAiService
{
    /// <summary>
    /// Gets a text response from the AI model.
    /// </summary>
    /// <param name="promptType">The type of prompt template to use.</param>
    /// <param name="query">The user query/content.</param>
    /// <param name="fastResponse">If true, uses a faster/cheaper model. If false, uses a more capable model.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The AI-generated response text.</returns>
    /// <exception cref="ArgumentException">Thrown when query exceeds maximum length or contains invalid content.</exception>
    /// <exception cref="InvalidOperationException">Thrown when API is not configured or request fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
    Task<string> GetResponseAsync(
        OpenAIPrompts promptType,
        string query,
        bool fastResponse = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a text response with a custom system prompt.
    /// </summary>
    /// <param name="systemPrompt">The system prompt that sets the AI's behavior.</param>
    /// <param name="userMessage">The user message/query.</param>
    /// <param name="fastResponse">If true, uses a faster/cheaper model.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The AI-generated response text.</returns>
    Task<string> GetResponseWithSystemPromptAsync(
        string systemPrompt,
        string userMessage,
        bool fastResponse = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed response information including token usage.
    /// </summary>
    /// <param name="promptType">The type of prompt template to use.</param>
    /// <param name="query">The user query/content.</param>
    /// <param name="fastResponse">If true, uses a faster/cheaper model.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Detailed response including text and metadata.</returns>
    Task<OpenAiResponse> GetDetailedResponseAsync(
        OpenAIPrompts promptType,
        string query,
        bool fastResponse = true,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Detailed response from the AI service including metadata.
/// </summary>
public sealed record OpenAiResponse
{
    /// <summary>
    /// The generated text content.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// The model used for the response.
    /// </summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// Number of prompt tokens used.
    /// </summary>
    public int PromptTokens { get; init; }

    /// <summary>
    /// Number of completion tokens used.
    /// </summary>
    public int CompletionTokens { get; init; }

    /// <summary>
    /// Total tokens used (prompt + completion).
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Time taken for the API call.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether the response was served from cache.
    /// </summary>
    public bool FromCache { get; init; }
}
