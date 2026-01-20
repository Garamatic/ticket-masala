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
        _apiKey = options.Value.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        _baseUrl = options.Value.BaseUrl;

        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("OpenAI API key not configured. Set 'OpenAI:ApiKey' in appsettings or 'OPENAI_API_KEY' environment variable.");
        }
    }

    public async Task<string> GetResponseAsync(OpenAIPrompts promptType, string query, bool fastResponse = true)
    {
        var client = string.IsNullOrEmpty(_baseUrl) 
            ? new OpenAIClient(_apiKey)
            : new OpenAIClient(_apiKey, new OpenAIClientOptions { Endpoint = new Uri(_baseUrl) });
            
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
