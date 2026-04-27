using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.Configuration;

public class OpenAiSettings
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
}
