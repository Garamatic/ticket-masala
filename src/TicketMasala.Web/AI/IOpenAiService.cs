namespace TicketMasala.Web.AI;

public interface IOpenAiService
{
    Task<string> GetResponseAsync(OpenAIPrompts promptType, string query, bool fastResponse = true);
}
