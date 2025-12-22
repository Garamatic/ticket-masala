using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.Configuration;

public class MasalaOptions
{
    public const string SectionName = "Masala";

    [Required]
    public string ConfigPath { get; set; } = "./config";

    public DatabaseOptions Database { get; set; } = new();

    public GerdaOptions Gerda { get; set; } = new();

    public FeatureOptions Features { get; set; } = new();
}

public class DatabaseOptions
{
    public string Provider { get; set; } = "Sqlite";
    public string ConnectionString { get; set; } = "Data Source=app.db";
}

public class GerdaOptions
{
    public bool Enabled { get; set; } = true;
    public string ModelPath { get; set; } = "./models";
    public string OpenAiModel { get; set; } = "gpt-4";
}

public class FeatureOptions
{
    public bool TicketGenerator { get; set; } = false;
    public int TicketGeneratorIntervalSeconds { get; set; } = 30;
    public bool EmailNotifications { get; set; } = false;
    public bool KnowledgeBase { get; set; } = true;
}
