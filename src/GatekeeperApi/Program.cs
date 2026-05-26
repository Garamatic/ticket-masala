using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GatekeeperApi;

/// <summary>
/// Minimal API for ingesting external tickets.
/// Publishes events directly to RabbitMQ for durable, scalable processing.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register the RabbitMQ publisher for direct event publishing
        builder.Services.AddSingleton<RabbitMqPublisher>();

        var app = builder.Build();

        var apiKey = builder.Configuration["Gatekeeper:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "Gatekeeper:ApiKey must be configured. Set it via environment variable or appsettings.json");
        }

        // Configure request size limit (10MB to prevent abuse)
        app.Use(async (context, next) =>
        {
            var originalBody = context.Request.Body;
            var bufferedStream = new Microsoft.AspNetCore.WebUtilities.FileBufferingReadStream(
                originalBody, 10 * 1024 * 1024, null, Path.GetTempPath());

            context.Request.Body = bufferedStream;
            try
            {
                await next();
            }
            finally
            {
                context.Request.Body = originalBody;
                await bufferedStream.DisposeAsync();
            }
        });

        app.MapPost("/api/ingest", async (
            HttpContext context,
            RabbitMqPublisher publisher,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedValue) ||
                extractedValue != apiKey)
            {
                logger.LogWarning("Unauthorized ingestion attempt from {RemoteIp}",
                    context.Connection.RemoteIpAddress);
                return Results.Unauthorized();
            }

            IngestionRequest? request;
            try
            {
                request = await context.Request.ReadFromJsonAsync<IngestionRequest>(ct);
            }
            catch (System.Text.Json.JsonException ex)
            {
                logger.LogWarning(ex, "Malformed JSON in ingestion request from {RemoteIp}",
                    context.Connection.RemoteIpAddress);
                return Results.BadRequest("Invalid JSON format");
            }

            if (request == null)
            {
                logger.LogWarning("Null ingestion request from {RemoteIp}",
                    context.Connection.RemoteIpAddress);
                return Results.BadRequest("Request body is required");
            }

            if (request.Data == null || request.Data.Count == 0)
            {
                logger.LogWarning("Ingestion request with empty data from {RemoteIp}",
                    context.Connection.RemoteIpAddress);
                return Results.BadRequest("Data dictionary is required and cannot be empty");
            }

            // Build the integration event payload matching the ticket.created schema
            var ticketId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("O");

            var evt = new TicketCreatedEvent
            {
                EventType = "ticket.created",
                Timestamp = now,
                Source = "gatekeeper-api",
                TicketId = ticketId,
                CustomerEmail = request.Data.GetValueOrDefault("email")?.ToString()
                    ?? request.Data.GetValueOrDefault("CustomerEmail")?.ToString()
                    ?? "unknown@example.com",
                CustomerName = request.Data.GetValueOrDefault("name")?.ToString()
                    ?? request.Data.GetValueOrDefault("CustomerName")?.ToString()
                    ?? "External User",
                TenantId = request.Data.GetValueOrDefault("tenant_id")?.ToString()
                    ?? request.Data.GetValueOrDefault("TenantId")?.ToString()
                    ?? string.Empty,
                Description = request.Data.GetValueOrDefault("description")?.ToString()
                    ?? request.Data.GetValueOrDefault("body")?.ToString()
                    ?? request.Data.GetValueOrDefault("subject")?.ToString()
                    ?? $"New ticket via {request.Template}",
                Priority = request.Data.GetValueOrDefault("priority")?.ToString()?.ToLowerInvariant()
                    ?? "medium",
                CreatedAt = now
            };

            try
            {
                await publisher.PublishAsync(evt, "event.ticket.created", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to publish ticket.created event for ingestion from {RemoteIp}. " +
                    "RabbitMQ may be unavailable.",
                    context.Connection.RemoteIpAddress);
                return Results.StatusCode(503); // Service Unavailable — caller can retry
            }

            logger.LogInformation(
                "Published ticket.created event: TicketId={TicketId}, Template={Template}, " +
                "RemoteIp={RemoteIp}",
                ticketId,
                request.Template,
                context.Connection.RemoteIpAddress);

            return Results.Accepted();
        });

        app.Run();
    }
}

/// <summary>
/// Request model for ingestion API.
/// </summary>
public class IngestionRequest
{
    public string Template { get; set; } = "default";
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Flat snake_case event matching integration-contracts schema for ticket.created.
/// </summary>
public record TicketCreatedEvent
{
    public string EventType { get; init; } = "ticket.created";
    public string Timestamp { get; init; } = string.Empty;
    public string Source { get; init; } = "gatekeeper-api";
    public string TicketId { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Priority { get; init; } = "medium";
    public string CreatedAt { get; init; } = string.Empty;
}
