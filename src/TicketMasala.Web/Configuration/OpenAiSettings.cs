using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.Configuration;

public class OpenAiSettings
{
    public const string SectionName = "OpenAI";

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
