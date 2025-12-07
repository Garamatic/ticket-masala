using System.Data;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using OpenAI;
using OpenAI.Chat;
using IT_Project2526.Models;
namespace IT_Project2526.AI
{
    public class OpenAiAPIHandler
    {
        public async Task<string> GetOpenAIResponse(OpenAIPrompts promptType, string question,bool fastResponse = true)
        {
            var client = new OpenAIClient(apiKey: LocalCache.AI_API_KEY);

            var model = fastResponse ? "gpt-4.1-mini" : "gpt-4.1";
            var chatClient = client.GetChatClient(model);
            
            var response = await chatClient.CompleteChatAsync(CreatePrompt(question,promptType));
            var chatContent = response.Value.Content;
            
           string answer = string.Join("", chatContent.Where(p => p.Text != null)
            .Select(p => p.Text));
            return answer;
        }
        //TODO extend this to get more precise prompts
        public static string CreatePrompt(string question, OpenAIPrompts promptType) 
        {
            return question;
        }
    }
  
}
