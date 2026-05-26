using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using Polly;
using Polly.Retry;
using TicketMasala.Domain.Ports;
using TicketMasala.Web.Configuration;

namespace TicketMasala.Web.AI;

/// <summary>
/// Production adapter for <see cref="IAIGenerationPort"/>.
/// Hides all provider-specific complexity: API key resolution, retry, caching,
/// input validation, prompt injection detection, OpenAI SDK vs OpenRouter,
/// token logging, and model selection.
/// </summary>
public sealed class OpenAIGenerationAdapter : IAIGenerationPort
{
    private readonly OpenAIClient _openAiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenAIGenerationAdapter> _logger;
    private readonly OpenAiSettings _settings;
    private readonly AIOperationRegistry _operationRegistry;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly string _fastModel;
    private readonly string _qualityModel;

    // Limits for input validation
    private const int MaxQueryLength = 8000;
    private const int CacheExpirationMinutes = 5;

    public OpenAIGenerationAdapter(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<OpenAIGenerationAdapter> logger,
        IOptions<OpenAiSettings> settings,
        IOptions<AIOperationRegistry> operationRegistry,
        IOptions<MasalaOptions>? masalaOptions = null)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
        _settings = settings.Value;
        _operationRegistry = operationRegistry.Value;

        // Resolve API key from env or config
        var apiKey = ResolveApiKey(_settings.ApiKey);
        var baseUrl = ResolveBaseUrl(_settings.BaseUrl, apiKey);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key is not configured. Adapter will return error messages.");
            _openAiClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential("dummy"));
        }
        else
        {
            var credential = new System.ClientModel.ApiKeyCredential(apiKey);
            var clientOptions = string.IsNullOrEmpty(baseUrl)
                ? null
                : new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };

            _openAiClient = clientOptions != null
                ? new OpenAIClient(credential, clientOptions)
                : new OpenAIClient(credential);
        }

        // Use configured models from MasalaOptions if available, else defaults
        var gerdaOptions = masalaOptions?.Value.Gerda;
        _fastModel = gerdaOptions?.OpenAiModelFast ?? "openai/gpt-4o-mini";
        _qualityModel = gerdaOptions?.OpenAiModel ?? "openai/gpt-4o";

        // Configure retry policy: 3 retries with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<InvalidOperationException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "OpenAI API call failed. Retry {RetryCount}/3 in {DelayMs}ms",
                        retryCount,
                        timeSpan.TotalMilliseconds);
                });
    }

    /// <inheritdoc />
    public async Task<AICompletion> CompleteAsync(
        AICompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(request.Content, MaxQueryLength, nameof(request.Content));

        // Resolve operation configuration
        if (!_operationRegistry.Operations.TryGetValue(request.Operation, out var operationConfig))
        {
            // Fallback: treat the operation as a passthrough with no special templating
            operationConfig = new AIOperationConfig
            {
                SystemTemplate = "You are a helpful assistant.",
                UserTemplate = "{content}",
                Tier = ModelTier.Default,
            };
        }

        var model = ResolveModel(operationConfig.Tier);
        var (systemPrompt, userPrompt) = BuildPrompts(operationConfig, request);
        var cacheKey = ComputeCacheKey(model, request.Operation, request.Content, request.Directive);

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out AICompletion? cachedResponse) && cachedResponse != null)
        {
            _logger.LogDebug("AI response served from cache for operation {Operation}", request.Operation);
            return cachedResponse with { FromCache = true };
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            return new AICompletion
            {
                Text = "Error: OpenAI API key is not configured. Please set 'OpenAI:ApiKey' in appsettings.json or 'OPENAI_API_KEY' environment variable.",
                Diagnostics = new Dictionary<string, object> { ["provider"] = "none", ["error"] = "missing_api_key" },
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _retryPolicy.ExecuteAsync(async ct =>
            {
                if (IsOpenRouter(model))
                {
                    var routerContent = await CallOpenRouterAsync(model, systemPrompt, userPrompt, ct);
                    return (Content: routerContent, PromptTokens: 0, CompletionTokens: 0);
                }

                var chatClient = _openAiClient.GetChatClient(model);
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt),
                };

                var chatResult = await chatClient.CompleteChatAsync(messages, cancellationToken: ct);
                var text = string.Join("", chatResult.Value.Content.Where(p => p.Text != null).Select(p => p.Text));

                return (
                    Content: text,
                    PromptTokens: chatResult.Value.Usage?.InputTokenCount ?? 0,
                    CompletionTokens: chatResult.Value.Usage?.OutputTokenCount ?? 0);
            }, cancellationToken);

            stopwatch.Stop();

            LogUsage(model, result.PromptTokens, result.CompletionTokens, stopwatch.Elapsed, false, request.Operation);

            var response = new AICompletion
            {
                Text = result.Content,
                FromCache = false,
                Diagnostics = new Dictionary<string, object>
                {
                    ["model"] = model,
                    ["provider"] = IsOpenRouter(model) ? "openrouter" : "openai",
                    ["prompt_tokens"] = result.PromptTokens,
                    ["completion_tokens"] = result.CompletionTokens,
                    ["total_tokens"] = result.PromptTokens + result.CompletionTokens,
                    ["duration_ms"] = stopwatch.Elapsed.TotalMilliseconds,
                    ["operation"] = request.Operation,
                },
            };

            // Cache the response
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(CacheExpirationMinutes));

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("AI request was cancelled by caller");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI API call failed after all retries for operation {Operation}", request.Operation);
            throw new InvalidOperationException($"Failed to get AI response for operation '{request.Operation}': {ex.Message}", ex);
        }
    }

    private static (string SystemPrompt, string UserPrompt) BuildPrompts(
        AIOperationConfig config, AICompletionRequest request)
    {
        var systemPrompt = config.SystemTemplate
            .Replace("{directive}", request.Directive ?? "", StringComparison.OrdinalIgnoreCase);

        var userPrompt = config.UserTemplate != null
            ? config.UserTemplate.Replace("{content}", request.Content, StringComparison.OrdinalIgnoreCase)
            : request.Content;

        return (systemPrompt, userPrompt);
    }

    private string ResolveModel(ModelTier tier)
    {
        return tier switch
        {
            ModelTier.Fast => _fastModel,
            ModelTier.Quality => _qualityModel,
            _ => _fastModel,
        };
    }

    private static void ValidateInput(string input, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be empty or whitespace.", paramName);
        }

        if (input.Length > maxLength)
        {
            throw new ArgumentException($"Input exceeds maximum length of {maxLength} characters.", paramName);
        }

        // Basic prompt injection detection
        var dangerousPatterns = new[] { "<script", "javascript:", "ignore previous", "ignore all previous", "disregard" };
        foreach (var pattern in dangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Input contains potentially dangerous content pattern: '{pattern}'", paramName);
            }
        }
    }

    private async Task<string> CallOpenRouterAsync(
        string model, string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("OpenRouter");

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
            temperature = 0.2,
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("chat/completions", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"OpenRouter error: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var message = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return message ?? string.Empty;
    }

    private void LogUsage(
        string model, int promptTokens, int completionTokens, TimeSpan duration, bool fromCache, string operation)
    {
        _logger.LogInformation(
            "AI call completed: Operation={Operation}, Model={Model}, PromptTokens={PromptTokens}, " +
            "CompletionTokens={CompletionTokens}, TotalTokens={TotalTokens}, Duration={DurationMs}ms, FromCache={FromCache}",
            operation,
            model,
            promptTokens,
            completionTokens,
            promptTokens + completionTokens,
            duration.TotalMilliseconds,
            fromCache);
    }

    private static string ComputeCacheKey(string model, string operation, string content, string? directive)
    {
        var raw = $"{model}:{operation}:{content}:{directive ?? ""}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes)[..32]; // First 32 chars of hex
    }

    private static bool IsOpenRouter(string model)
    {
        return model.Contains('/', StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveBaseUrl(string? configuredBaseUrl, string apiKey)
    {
        var envBaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        var baseUrl = !string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? configuredBaseUrl
            : envBaseUrl;

        if (string.IsNullOrWhiteSpace(baseUrl) && apiKey.StartsWith("sk-or-", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "https://openrouter.ai/api/v1";
        }

        return NormalizeBaseUrl(baseUrl);
    }

    private static string? NormalizeBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return baseUrl;

        var normalized = baseUrl.TrimEnd('/');

        // OpenAI SDK appends /v1; avoid double /v1
        if (normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^3];
        }

        return normalized;
    }

    private static string ResolveApiKey(string configuredKey)
    {
        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        var sanitizedConfiguredKey = string.IsNullOrWhiteSpace(configuredKey) || configuredKey == "placeholder-key"
            ? null
            : configuredKey;

        return !string.IsNullOrWhiteSpace(envKey)
            ? envKey
            : (sanitizedConfiguredKey ?? "");
    }
}
