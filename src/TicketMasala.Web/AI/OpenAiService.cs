using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TicketMasala.Web.AI;

public class OpenAiService : IOpenAiService
{
    private readonly string _apiKey;
    private readonly string? _baseUrl;

    public OpenAiService(IOptions<Configuration.OpenAiSettings> options)
    {
        var configuredKey = options.Value.ApiKey;
        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var configuredBaseUrl = options.Value.BaseUrl;
        var envBaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");

        _apiKey = !string.IsNullOrWhiteSpace(configuredKey)
            ? configuredKey
            : (envKey ?? "");
        var baseUrl = !string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? configuredBaseUrl
            : envBaseUrl;

        if (string.IsNullOrWhiteSpace(baseUrl) && _apiKey.StartsWith("sk-or-", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "https://openrouter.ai/api/v1";
        }

        _baseUrl = NormalizeBaseUrl(baseUrl);
    }

    private static string? NormalizeBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return baseUrl;

        var normalized = baseUrl.TrimEnd('/');

        // OpenAI SDK appends /v1; avoid double /v1 (e.g., openrouter.ai/api/v1/v1)
        if (normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^3];
        }

        return normalized;
    }

    public async Task<string> GetResponseAsync(OpenAIPrompts promptType, string query, bool fastResponse = true)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: OpenAI API key is not configured. Please set 'OpenAI:ApiKey' in appsettings.json or 'OPENAI_API_KEY' environment variable.";
        }

        var model = fastResponse ? "openai/gpt-4o-mini" : "openai/gpt-4o";
        var prompt = CreatePrompt(query, promptType);

        try
        {
            var client = string.IsNullOrEmpty(_baseUrl)
                ? new OpenAIClient(new System.ClientModel.ApiKeyCredential(_apiKey))
                : new OpenAIClient(new System.ClientModel.ApiKeyCredential(_apiKey), new OpenAIClientOptions { Endpoint = new Uri(_baseUrl) });

            var chatClient = client.GetChatClient(model);
            var response = await chatClient.CompleteChatAsync(prompt);
            var chatContent = response.Value.Content;

            return string.Join("", chatContent.Where(p => p.Text != null).Select(p => p.Text));
        }
        catch (JsonException) when (IsOpenRouter())
        {
            return await CallOpenRouterAsync(model, prompt);
        }
        catch (InvalidOperationException) when (IsOpenRouter())
        {
            return await CallOpenRouterAsync(model, prompt);
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
            _ => query,
        };
    }

    private bool IsOpenRouter()
    {
        return !string.IsNullOrWhiteSpace(_baseUrl) && _baseUrl.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase);
    }

    private string GetOpenRouterEndpoint()
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            return "https://openrouter.ai/api/v1/chat/completions";
        }

        var baseUrl = _baseUrl.TrimEnd('/');
        if (!baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = $"{baseUrl}/v1";
        }

        return $"{baseUrl}/chat/completions";
    }

    private async Task<string> CallOpenRouterAsync(string model, string prompt)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new
        {
            model,
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await http.PostAsync(GetOpenRouterEndpoint(), content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenRouter error: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
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
}
