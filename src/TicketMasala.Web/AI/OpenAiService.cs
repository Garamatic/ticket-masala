using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace TicketMasala.Web.AI;

public class OpenAiService : IOpenAiService
{
    private readonly string _apiKey;
    private readonly string? _baseUrl;

    public OpenAiService(IOptions<Configuration.OpenAiSettings> options)
    {
        var configuredKey = options.Value.ApiKey;
        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        _apiKey = !string.IsNullOrWhiteSpace(configuredKey)
            ? configuredKey
            : (envKey ?? "");
        _baseUrl = NormalizeBaseUrl(options.Value.BaseUrl);
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

        var client = string.IsNullOrEmpty(_baseUrl) 
            ? new OpenAIClient(new System.ClientModel.ApiKeyCredential(_apiKey))
            : new OpenAIClient(new System.ClientModel.ApiKeyCredential(_apiKey), new OpenAIClientOptions { Endpoint = new Uri(_baseUrl) });
            
        var model = fastResponse ? "openai/gpt-4o-mini" : "openai/gpt-4o";
        var chatClient = client.GetChatClient(model);

        var response = await chatClient.CompleteChatAsync(CreatePrompt(query, promptType));
        var chatContent = response.Value.Content;

        return string.Join("", chatContent.Where(p => p.Text != null).Select(p => p.Text));
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
}
