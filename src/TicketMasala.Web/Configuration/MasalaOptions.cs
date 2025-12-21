using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.Configuration;

/// <summary>
/// Strongly-typed configuration options for Ticket Masala.
/// Binds to the "Masala" section in appsettings.json.
/// </summary>
public class MasalaOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Masala";

    /// <summary>
    /// Base path for external configuration files.
    /// </summary>
    public string ConfigPath { get; set; } = string.Empty;

    /// <summary>
    /// Database configuration options.
    /// </summary>
    [Required]
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// GERDA AI configuration options.
    /// </summary>
    public GerdaOptions Gerda { get; set; } = new();

    /// <summary>
    /// Feature flags for optional functionality.
    /// </summary>
    public FeatureFlags Features { get; set; } = new();
}


/// <summary>
/// Database configuration options.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Database provider type (Sqlite, SqlServer).
    /// </summary>
    [Required]
    [RegularExpression("^(Sqlite|SqlServer)$", ErrorMessage = "Provider must be 'Sqlite' or 'SqlServer'")]
    public string Provider { get; set; } = "Sqlite";

    /// <summary>
    /// Database connection string.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = "Data Source=app.db";
}

/// <summary>
/// GERDA AI configuration options.
/// </summary>
public class GerdaOptions
{
    /// <summary>
    /// Whether GERDA AI features are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Path to ML model files.
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI API key for AI-powered features.
    /// </summary>
    public string? OpenAiApiKey { get; set; }

    /// <summary>
    /// OpenAI model to use (e.g., gpt-4, gpt-3.5-turbo).
    /// </summary>
    public string OpenAiModel { get; set; } = "gpt-4";
}

/// <summary>
/// Feature flags for optional functionality.
/// </summary>
public class FeatureFlags
{
    /// <summary>
    /// Whether the automatic ticket generator is enabled.
    /// </summary>
    public bool TicketGenerator { get; set; } = false;

    /// <summary>
    /// Interval in seconds between ticket generation cycles.
    /// </summary>
    [Range(5, 3600, ErrorMessage = "TicketGeneratorIntervalSeconds must be between 5 and 3600")]
    public int TicketGeneratorIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Whether email notifications are enabled.
    /// </summary>
    public bool EmailNotifications { get; set; } = false;

    /// <summary>
    /// Whether the knowledge base feature is enabled.
    /// </summary>
    public bool KnowledgeBase { get; set; } = true;
}
