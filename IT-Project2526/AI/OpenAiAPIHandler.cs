using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenAI;
using OpenAI.Chat;
using IT_Project2526.Models;

namespace IT_Project2526.AI
{
    public class OpenAiAPIHandler
    {
        public static async Task<string> GetOpenAIResponse(OpenAIPrompts promptType, string query, bool fastResponse = true)
        {
            try
            {
                var client = new OpenAIClient(apiKey: LocalCache.AI_API_KEY);

                var model = fastResponse ? "gpt-4.1-mini" : "gpt-4.1";
                var chatClient = client.GetChatClient(model);

                var prompt = CreatePrompt(query, promptType);

                var response = await chatClient.CompleteChatAsync(prompt);
                var chatContent = response.Value.Content;

                string answer = string.Join("",
                    chatContent.Where(p => p.Text != null).Select(p => p.Text));

                if (promptType == OpenAIPrompts.Steps)
                    return FormatSteps(answer);

                return answer;
            }
            catch
            {
                return "";
            }
        }

        private static string CreatePrompt(string query, OpenAIPrompts promptType)
        {
            switch (promptType)
            {
                case OpenAIPrompts.Normal:
                    return query;

                case OpenAIPrompts.Steps:
                    return $@"
                        Please break down the following task into clear step-by-step instructions:
                            {query}
                        Return ONLY a JSON array of steps. No explanations, no markdown, no commentary.
                        Example:
                        [
                        ""First do X..."",
                        ""Then do Y...""
                        ]
                        ";

                case OpenAIPrompts.Quick:
                    return $"Provide a concise answer for: {query}. Do not give follow up questions.";

                case OpenAIPrompts.Detailed:
                    return $"Provide a detailed and thorough explanation of: {query}. Do not give follow up questions.";

                case OpenAIPrompts.ProsCons:
                    return $"List the pros and cons of: {query}. Do not give follow up questions.";

                case OpenAIPrompts.Summary:
                    return $"Summarize the key points about: {query}. Do not give follow up questions.";

                default:
                    return query;
            }
        }

        private static string FormatSteps(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            try
            {
                var steps = JsonSerializer.Deserialize<string[]>(raw);
                if (steps != null && steps.Any())
                {
                    return string.Join(Environment.NewLine + Environment.NewLine,
                        steps.Select((s, i) => $"###{i + 1} {s.Trim()}"));
                }
            }
            catch
            {
                return "";
            }

            var parts = Regex.Split(raw, @"(?=###)")
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (parts.Count > 1)
                return string.Join(Environment.NewLine + Environment.NewLine, parts);

            parts = Regex.Split(raw, @"(?:(?:\n|^)\s*\d+\.\s+)")
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (parts.Count > 1)
            {
                return string.Join(Environment.NewLine + Environment.NewLine,
                    parts.Select((p, i) => $"###{i + 1} {p}"));
            }

            var sentences = Regex.Split(raw, @"(?<=[\.!\?])\s+(?=[A-Z])")
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            return string.Join(Environment.NewLine + Environment.NewLine,
                sentences.Select((p, i) => $"###{i + 1} {p}"));
        }
    }
}
