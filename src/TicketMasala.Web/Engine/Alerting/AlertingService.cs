using System.Text;
using System.Text.Json;

namespace TicketMasala.Web.Engine.Alerting;

/// <summary>
/// Service for sending webhook alerts for operational events.
/// KISS approach: simple HTTP POST with JSON payload.
/// </summary>
public interface IAlertingService
{
    Task SendAlertAsync(string alertType, string message, object? data = null);
}

/// <summary>
/// Webhook configuration
/// </summary>
public class WebhookConfig
{
    public string? Url { get; set; }
    public string[]? AlertTypes { get; set; } // null = all types
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Simple webhook alerting service
/// </summary>
public class AlertingService : IAlertingService
{
    private readonly ILogger<AlertingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly List<WebhookConfig> _webhooks;

    public AlertingService(
        ILogger<AlertingService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _webhooks = LoadWebhooks(configuration);
    }

    private List<WebhookConfig> LoadWebhooks(IConfiguration configuration)
    {
        var webhooks = new List<WebhookConfig>();
        var section = configuration.GetSection("Alerting:Webhooks");
        
        foreach (var child in section.GetChildren())
        {
            var webhook = new WebhookConfig
            {
                Url = child["Url"],
                Enabled = child.GetValue("Enabled", true),
                AlertTypes = child.GetSection("AlertTypes").Get<string[]>()
            };
            
            if (!string.IsNullOrEmpty(webhook.Url))
            {
                webhooks.Add(webhook);
                _logger.LogInformation("Registered webhook for {Types}", 
                    webhook.AlertTypes != null ? string.Join(",", webhook.AlertTypes) : "all alerts");
            }
        }
        
        return webhooks;
    }

    public async Task SendAlertAsync(string alertType, string message, object? data = null)
    {
        var payload = new
        {
            type = alertType,
            message = message,
            timestamp = DateTime.UtcNow,
            data = data
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var relevantWebhooks = _webhooks
            .Where(w => w.Enabled)
            .Where(w => w.AlertTypes == null || w.AlertTypes.Contains(alertType, StringComparer.OrdinalIgnoreCase));

        var client = _httpClientFactory.CreateClient("Alerting");
        client.Timeout = TimeSpan.FromSeconds(10);

        foreach (var webhook in relevantWebhooks)
        {
            if (string.IsNullOrEmpty(webhook.Url)) continue;
            
            try
            {
                var response = await client.PostAsync(webhook.Url, content);
                _logger.LogDebug("Sent alert to {Url}: {Status}", webhook.Url, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send alert to {Url}", webhook.Url);
            }
        }
    }
}

/// <summary>
/// Common alert types
/// </summary>
public static class AlertTypes
{
    public const string TicketCreated = "ticket.created";
    public const string TicketAssigned = "ticket.assigned";
    public const string TicketCompleted = "ticket.completed";
    public const string GerdaError = "gerda.error";
    public const string HealthDegraded = "health.degraded";
    public const string ConfigReloaded = "config.reloaded";
}
