using System.Net.Http.Json;
using System.Text.Json;

namespace GatekeeperApi;

/// <summary>
/// Defines the contract for processing ingestion requests.
/// </summary>
public interface IIngestionProcessor
{
    Task ProcessAsync(IngestionRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of IIngestionProcessor that forwards requests to TicketMasala.Web via HTTP.
/// </summary>
public class HttpIngestionProcessor : IIngestionProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpIngestionProcessor> _logger;
    private readonly string _apiKey;

    public HttpIngestionProcessor(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<HttpIngestionProcessor> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = config["MasalaConnection:BaseUrl"] ?? "http://localhost:5080";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _apiKey = config["MasalaConnection:ApiKey"] ?? string.Empty;
    }

    public async Task ProcessAsync(IngestionRequest request, CancellationToken cancellationToken)
    {
        // Simple mapping from generic Dictionary to expected ExternalTicketRequest fields
        // In a real scenario, this would use the 'Template' field to decide how to map.
        var externalRequest = new
        {
            CustomerEmail = request.Data.GetValueOrDefault("email")?.ToString() ?? request.Data.GetValueOrDefault("CustomerEmail")?.ToString() ?? "unknown@example.com",
            CustomerName = request.Data.GetValueOrDefault("name")?.ToString() ?? request.Data.GetValueOrDefault("CustomerName")?.ToString() ?? "External User",
            Subject = request.Data.GetValueOrDefault("subject")?.ToString() ?? request.Data.GetValueOrDefault("Subject")?.ToString() ?? $"New ticket via {request.Template}",
            Description = request.Data.GetValueOrDefault("description")?.ToString() ?? request.Data.GetValueOrDefault("body")?.ToString() ?? "No description provided",
            SourceSite = request.Template
        };

        _logger.LogInformation("Forwarding request to TicketMasala.Web: {Subject}", externalRequest.Subject);

        // Call the external submission endpoint
        // Note: Using V1 version as seen in the controller
        var response = await _httpClient.PostAsJsonAsync("api/v1.0/tickets/external", externalRequest, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully delivered request to TicketMasala.Web");
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to deliver request to TicketMasala.Web. Status: {Status}, Error: {Error}",
                response.StatusCode, error);

            // Note: In a production system, we might want to throw here to trigger retry logic 
            // if the worker supports it, or move to a Dead Letter Queue.
        }
    }
}
