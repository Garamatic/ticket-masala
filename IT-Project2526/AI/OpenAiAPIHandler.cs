using System.Data;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using OpenAI;
using OpenAI.Chat;
using IT_Project2526.Models;
namespace IT_Project2526.AI
{
    public class OpenAiAPIHandler
    {
        public static async Task<string> GetOpenAIResponse(OpenAIPrompts promptType, string query, bool fastResponse = true)
        {
            var client = new OpenAIClient(apiKey: LocalCache.AI_API_KEY);

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
}
