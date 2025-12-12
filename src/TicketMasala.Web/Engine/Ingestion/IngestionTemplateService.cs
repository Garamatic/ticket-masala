using Scriban;
using Scriban.Runtime;

namespace TicketMasala.Web.Engine.Ingestion;

/// <summary>
/// Service for transforming external data using Scriban templates.
/// Enables no-code ingestion mapping via YAML configuration.
/// </summary>
public interface IIngestionTemplateService
{
    /// <summary>
    /// Transforms source data using a named ingestion template
    /// </summary>
    IngestionResult Transform(string templateName, Dictionary<string, object> sourceData);

    /// <summary>
    /// Lists available ingestion templates
    /// </summary>
    IEnumerable<string> GetTemplateNames();
}

/// <summary>
/// Result of ingestion template transformation
/// </summary>
public class IngestionResult
{
    public bool Success { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string DomainId { get; set; } = "IT";
    public string? CustomerId { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
    public string? Error { get; set; }
}

/// <summary>
/// KISS implementation using inline templates from config
/// </summary>
public class IngestionTemplateService : IIngestionTemplateService
{
    private readonly ILogger<IngestionTemplateService> _logger;
    private readonly Dictionary<string, IngestionTemplate> _templates;

    public IngestionTemplateService(
        ILogger<IngestionTemplateService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _templates = new Dictionary<string, IngestionTemplate>();

        // Load templates from configuration (appsettings.json or env vars)
        LoadTemplatesFromConfig(configuration);
    }

    private void LoadTemplatesFromConfig(IConfiguration configuration)
    {
        // Load from appsettings.json section "IngestionTemplates"
        var section = configuration.GetSection("IngestionTemplates");

        foreach (var child in section.GetChildren())
        {
            var template = new IngestionTemplate
            {
                Name = child.Key,
                TitleTemplate = child["Title"] ?? "{{ source.subject }}",
                DescriptionTemplate = child["Description"] ?? "{{ source.body }}",
                DomainId = child["DomainId"] ?? "IT",
                CustomerIdTemplate = child["CustomerId"]
            };

            // Load custom field mappings
            var fields = child.GetSection("CustomFields");
            foreach (var field in fields.GetChildren())
            {
                template.CustomFieldTemplates[field.Key] = field.Value ?? "";
            }

            _templates[child.Key] = template;
            _logger.LogInformation("Loaded ingestion template: {Name}", child.Key);
        }

        // Add default template if none configured
        if (_templates.Count == 0)
        {
            _templates["default"] = new IngestionTemplate
            {
                Name = "default",
                TitleTemplate = "{{ source.subject | string.truncate 100 }}",
                DescriptionTemplate = "{{ source.body }}",
                DomainId = "IT"
            };
            _logger.LogInformation("Using default ingestion template");
        }
    }

    public IngestionResult Transform(string templateName, Dictionary<string, object> sourceData)
    {
        if (!_templates.TryGetValue(templateName, out var template))
        {
            template = _templates.Values.FirstOrDefault() ?? throw new InvalidOperationException("No templates configured");
        }

        try
        {
            var scriptObject = new ScriptObject();
            scriptObject["source"] = sourceData;

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            var result = new IngestionResult
            {
                Success = true,
                Title = RenderTemplate(template.TitleTemplate, context),
                Description = RenderTemplate(template.DescriptionTemplate, context),
                DomainId = template.DomainId
            };

            if (!string.IsNullOrEmpty(template.CustomerIdTemplate))
            {
                result.CustomerId = RenderTemplate(template.CustomerIdTemplate, context);
            }

            foreach (var (key, fieldTemplate) in template.CustomFieldTemplates)
            {
                result.CustomFields[key] = RenderTemplate(fieldTemplate, context);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transform using template {Name}", templateName);
            return new IngestionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private static string RenderTemplate(string templateText, TemplateContext context)
    {
        var template = Template.Parse(templateText);
        return template.Render(context).Trim();
    }

    public IEnumerable<string> GetTemplateNames() => _templates.Keys;
}

/// <summary>
/// Configuration for an ingestion template
/// </summary>
internal class IngestionTemplate
{
    public string Name { get; set; } = "";
    public string TitleTemplate { get; set; } = "";
    public string DescriptionTemplate { get; set; } = "";
    public string DomainId { get; set; } = "IT";
    public string? CustomerIdTemplate { get; set; }
    public Dictionary<string, string> CustomFieldTemplates { get; set; } = new();
}
