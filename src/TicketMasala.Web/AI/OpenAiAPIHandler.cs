using OpenAI;
using OpenAI.Chat;
using TicketMasala.Web.Models;

namespace TicketMasala.Web.AI;

/// <summary>
/// Handler for OpenAI API calls for ticket and project intelligence.
/// </summary>
public class OpenAiAPIHandler
{
    /// <summary>
    /// Get a response from OpenAI based on the prompt type
    /// </summary>
    /// <param name="promptType">Type of prompt to use</param>
    /// <param name="query">The query/content to process</param>
    /// <param name="fastResponse">Use faster model (gpt-4.1-mini) vs full model</param>
    /// <returns>AI-generated response</returns>
    public static async Task<string> GetOpenAIResponse(OpenAIPrompts promptType, string query, bool fastResponse = true)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY environment variable.");
        }

        var client = new OpenAIClient(apiKey: apiKey);

        var model = fastResponse ? "gpt-4.1-mini" : "gpt-4.1";
        var chatClient = client.GetChatClient(model);

        var response = await chatClient.CompleteChatAsync(CreatePrompt(query, promptType));
        var chatContent = response.Value.Content;

        string answer = string.Join("", chatContent.Where(p => p.Text != null)
         .Select(p => p.Text));
        return answer;
    }

    private static string CreatePrompt(string query, OpenAIPrompts promptType)
    {
        switch (promptType)
        {
            case OpenAIPrompts.Normal:
                return query;

            case OpenAIPrompts.Steps:
                return $"Please explain step by step: {query}";

            case OpenAIPrompts.Quick:
                return $"Provide a concise answer for: {query}";

            case OpenAIPrompts.Detailed:
                return $"Provide a detailed and thorough explanation of: {query}";

            case OpenAIPrompts.ProsCons:
                return $"List the pros and cons of: {query}";

            case OpenAIPrompts.Summary:
                return $"Summarize the key points about: {query}";

            default:
                return query;
        }
    }
}

/// <summary>
/// Available OpenAI prompt types
/// </summary>
public enum OpenAIPrompts
{
    Normal,
    Steps,
    Quick,
    Detailed,
    ProsCons,
    Summary
}
