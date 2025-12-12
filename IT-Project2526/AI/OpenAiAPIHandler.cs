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
                if (promptType == OpenAIPrompts.Summary)
                    return FormatSummary(answer);
                if (promptType == OpenAIPrompts.ProsCons)
                    return FormatProsCons(answer);

                return answer;
            }
            catch
            {
                return "";
            }
        }


        private static string CreatePrompt(string query, OpenAIPrompts promptType)
        {
            return promptType switch
            {
                OpenAIPrompts.Normal =>
                    query,

                OpenAIPrompts.Steps =>
                    $@"
                    Break down this task into clear step-by-step instructions:
                    {query}
                    Return ONLY a JSON array of steps. No explanations or markdown.
                    Example:
                    [
                      ""First do X..."",
                      ""Then do Y...""
                    ]
                    ",

                OpenAIPrompts.Quick =>
                    $"Provide a concise answer for: {query}. Do not give follow up questions.",

                OpenAIPrompts.Detailed =>
                    $"Provide a detailed explanation of: {query}. Do not give follow up questions.",

                OpenAIPrompts.ProsCons =>
                    $"List the pros and cons of: {query}. Return JSON in this exact format: {{\"pros\":[...], \"cons\":[...]}}",

                OpenAIPrompts.Summary =>
                    $"Summarize the key points about: {query}. Return ONLY a JSON array of bullet points.",

                _ => query
            };
        }
        private static string CleanJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            raw = raw.Trim();

            raw = Regex.Replace(raw, @"^```[a-zA-Z0-9]*", "", RegexOptions.Multiline).Trim();
            raw = Regex.Replace(raw, @"```$", "", RegexOptions.Multiline).Trim();

            int firstBracket = raw.IndexOf('[');
            if (firstBracket > 0)
                raw = raw.Substring(firstBracket);

            int lastBracket = raw.LastIndexOf(']');
            if (lastBracket > 0)
                raw = raw.Substring(0, lastBracket + 1);

            return raw.Trim();
        }
        private static string FormatSteps(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            try
            {
                var cleanJson = CleanJson(raw);
                var steps = JsonSerializer.Deserialize<string[]>(cleanJson);
                if (steps != null && steps.Any())
                {
                    return string.Join(Environment.NewLine + Environment.NewLine,
                        steps.Select((s, i) => $"###{i + 1} {s.Trim()}"));
                }
            }
            catch
            {
                // fall through
            }

            return raw;
        }

        private static string FormatSummary(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            try
            {
                var cleanJson = CleanJson(raw);
                var points = JsonSerializer.Deserialize<string[]>(cleanJson);
                if (points != null && points.Any())
                {
                    return string.Join(Environment.NewLine,
                        points.Select(p => $"• {p.Trim()} \n"));
                }
            }
            catch
            {
                return "No summary";
            }

            return raw;
        }

        private static string FormatProsCons(string raw)
        {
            try
            {
                var cleanJson = CleanJson(raw);
                var json = JsonDocument.Parse(cleanJson);

                var pros = json.RootElement.GetProperty("pros").EnumerateArray()
                    .Select(p => p.GetString())
                    .Where(p => p != null)
                    .ToList();

                var cons = json.RootElement.GetProperty("cons").EnumerateArray()
                    .Select(p => p.GetString())
                    .Where(p => p != null)
                    .ToList();

                string formatted =
                    "### Pros\n" +
                    string.Join(Environment.NewLine, pros.Select(p => $"• {p}")) +
                    "\n\n### Cons\n" +
                    string.Join(Environment.NewLine, cons.Select(p => $"• {p}"));

                return formatted;
            }
            catch
            {
                return raw;
            }
        }
    }
}
