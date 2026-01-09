using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

namespace TicketMasala.Web.AI;

public class OpenAiService : IOpenAiService
{
    private readonly string _apiKey;

    public OpenAiService(IConfiguration configuration)
    {
        // Preferred: configuration.GetSection("OpenAI:ApiKey").Value
        // Fallback: Environment variable (for backward compatibility during migration)
        _apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            // Log warning or throw, depending on policy. For now, throw to match previous behavior.
            throw new InvalidOperationException("OpenAI API key not configured. Set 'OpenAI:ApiKey' in appsettings or 'OPENAI_API_KEY' environment variable.");
        }
    }

    public async Task<string> GetResponseAsync(OpenAIPrompts promptType, string query, bool fastResponse = true)
    {
        var client = new OpenAIClient(apiKey: _apiKey);
        var model = fastResponse ? "gpt-4.1-mini" : "gpt-4.1";
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
