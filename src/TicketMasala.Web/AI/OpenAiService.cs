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
using TicketMasala.Web.Configuration;

namespace TicketMasala.Web.AI;

/// <summary>
/// Production-ready implementation of IOpenAiService.
/// Features: client reuse, retry logic, caching, input validation, structured logging.
/// </summary>
public sealed class OpenAiService : IOpenAiService
{
    private readonly OpenAIClient _openAiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenAiService> _logger;
    private readonly OpenAiSettings _settings;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly string _fastModel;
    private readonly string _qualityModel;

    // Limits for input validation
    private const int MaxQueryLength = 8000;
    private const int MaxSystemPromptLength = 4000;
    private const int CacheExpirationMinutes = 5;

    /// <summary>
    /// Creates a new instance of OpenAiService.
    /// </summary>
    public OpenAiService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<OpenAiService> logger,
        IOptions<OpenAiSettings> settings,
        IOptions<Configuration.MasalaOptions>? masalaOptions = null)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
        _settings = settings.Value;

        // Resolve API key from env or config
        var apiKey = ResolveApiKey(_settings.ApiKey);
        var baseUrl = ResolveBaseUrl(_settings.BaseUrl, apiKey);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key is not configured. Service will return error messages.");
            // Create a dummy client that will never be used (we check for empty key before use)
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
    public async Task<string> GetResponseAsync(
        OpenAIPrompts promptType,
        string query,
        bool fastResponse = true,
        CancellationToken cancellationToken = default)
    {
        var response = await GetDetailedResponseAsync(promptType, query, fastResponse, cancellationToken);
        return response.Content;
    }

    /// <inheritdoc />
    public async Task<string> GetResponseWithSystemPromptAsync(
        string systemPrompt,
        string userMessage,
        bool fastResponse = true,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(userMessage, MaxQueryLength, nameof(userMessage));
        ValidateInput(systemPrompt, MaxSystemPromptLength, nameof(systemPrompt));

        var model = fastResponse ? _fastModel : _qualityModel;
        var cacheKey = ComputeCacheKey(model, systemPrompt, userMessage);

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out string? cachedResponse) && !string.IsNullOrEmpty(cachedResponse))
        {
            _logger.LogDebug("OpenAI response served from cache for model {Model}", model);
            return cachedResponse;
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            return "Error: OpenAI API key is not configured. Please set 'OpenAI:ApiKey' in appsettings.json or 'OPENAI_API_KEY' environment variable.";
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await _retryPolicy.ExecuteAsync(async ct =>
            {
                if (IsOpenRouter(model))
                {
                    return await CallOpenRouterWithSystemPromptAsync(model, systemPrompt, userMessage, ct);
                }

                var chatClient = _openAiClient.GetChatClient(model);
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userMessage)
                };

                var result = await chatClient.CompleteChatAsync(messages, cancellationToken: ct);
                var content = string.Join("", result.Value.Content.Where(p => p.Text != null).Select(p => p.Text));

                LogUsage(model, result.Value.Usage?.InputTokenCount ?? 0, result.Value.Usage?.OutputTokenCount ?? 0, stopwatch.Elapsed, false);

                return content;
            }, cancellationToken);

            // Cache the response
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(CacheExpirationMinutes));

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("OpenAI request was cancelled by caller");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API call failed after all retries");
            throw new InvalidOperationException($"Failed to get AI response: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OpenAiResponse> GetDetailedResponseAsync(
        OpenAIPrompts promptType,
        string query,
        bool fastResponse = true,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(query, MaxQueryLength, nameof(query));

        var prompt = CreatePrompt(query, promptType);
        var model = fastResponse ? _fastModel : _qualityModel;
        var cacheKey = ComputeCacheKey(model, promptType.ToString(), query);

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out OpenAiResponse? cachedResponse) && cachedResponse != null)
        {
            _logger.LogDebug("OpenAI detailed response served from cache for model {Model}", model);
            return cachedResponse with { FromCache = true };
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            return new OpenAiResponse
            {
                Content = "Error: OpenAI API key is not configured. Please set 'OpenAI:ApiKey' in appsettings.json or 'OPENAI_API_KEY' environment variable.",
                Model = model,
                Duration = TimeSpan.Zero
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _retryPolicy.ExecuteAsync(async ct =>
            {
                if (IsOpenRouter(model))
                {
                    var routerContent = await CallOpenRouterAsync(model, prompt, ct);
                    return (Content: routerContent, PromptTokens: 0, CompletionTokens: 0);
                }

                var chatClient = _openAiClient.GetChatClient(model);
                var chatResult = await chatClient.CompleteChatAsync(new[] { new UserChatMessage(prompt) }, cancellationToken: ct);
                var text = string.Join("", chatResult.Value.Content.Where(p => p.Text != null).Select(p => p.Text));

                return (Content: text, PromptTokens: chatResult.Value.Usage?.InputTokenCount ?? 0, CompletionTokens: chatResult.Value.Usage?.OutputTokenCount ?? 0);
            }, cancellationToken);

            stopwatch.Stop();

            var response = new OpenAiResponse
            {
                Content = result.Content,
                Model = model,
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,
                Duration = stopwatch.Elapsed,
                FromCache = false
            };

            LogUsage(model, result.PromptTokens, result.CompletionTokens, stopwatch.Elapsed, false);

            // Cache the response
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(CacheExpirationMinutes));

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("OpenAI request was cancelled by caller");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API call failed after all retries");
            throw new InvalidOperationException($"Failed to get AI response: {ex.Message}", ex);
        }
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

    private static string CreatePrompt(string query, OpenAIPrompts promptType)
    {
        return promptType switch
        {
            OpenAIPrompts.Normal => query,
            OpenAIPrompts.Steps => $"Please explain step by step: {query}",
            OpenAIPrompts.Quick => $"Provide a concise answer for: {query}",
            OpenAIPrompts.Detailed => $"Provide a detailed and thorough explanation of: {query}",
            OpenAIPrompts.ProsCons => $"List the pros and cons of: {query}",
            OpenAIPrompts.Summary => $"Summarize the key points about: {query}",
            _ => query
        };
    }

    private async Task<string> CallOpenRouterAsync(string model, string prompt, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("OpenRouter");

        var payload = new
        {
            model,
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.2
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

    private async Task<string> CallOpenRouterWithSystemPromptAsync(string model, string systemPrompt, string userMessage, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("OpenRouter");

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.2
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

    private void LogUsage(string model, int promptTokens, int completionTokens, TimeSpan duration, bool fromCache)
    {
        _logger.LogInformation(
            "OpenAI call completed: Model={Model}, PromptTokens={PromptTokens}, CompletionTokens={CompletionTokens}, " +
            "TotalTokens={TotalTokens}, Duration={DurationMs}ms, FromCache={FromCache}",
            model,
            promptTokens,
            completionTokens,
            promptTokens + completionTokens,
            duration.TotalMilliseconds,
            fromCache);
    }

    private static string ComputeCacheKey(string model, string promptType, string query)
    {
        // Simple hash for cache key
        var raw = $"{model}:{promptType}:{query}";
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
