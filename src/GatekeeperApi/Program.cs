using System.IO;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

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

        // Register the shared RabbitMQ publisher for direct event publishing
        builder.Services.AddSingleton<RabbitMqConnector.IRabbitMqPublisher, RabbitMqConnector.RabbitMqPublisher>();

        // OpenTelemetry tracing
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName: "GatekeeperApi",
                serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = httpContext =>
                    {
                        var path = httpContext.Request.Path.Value ?? "";
                        return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase);
                    };
                });
                t.AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                });
                t.AddOtlpExporter();
            });

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

        static IResult HealthCheck() => Results.Ok(new { status = "Healthy" });
        app.MapGet("/health", HealthCheck);
        app.MapGet("/api/health", HealthCheck);

        async Task<IResult> ProcessIngestionRequestAsync(
            HttpContext context,
            RabbitMqConnector.IRabbitMqPublisher publisher,
            ILogger<Program> logger,
            CancellationToken ct)
        {
            var remoteIp = context.Connection.RemoteIpAddress;

            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedValue) ||
                extractedValue != apiKey)
            {
                logger.LogWarning("Unauthorized ingestion attempt from {RemoteIp}", remoteIp);
                return Results.Unauthorized();
            }

            try
            {
                using var document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: ct);
                var root = document.RootElement;

                // Check if it's a direct flat event containing 'event_type'
                if (root.TryGetProperty("event_type", out var eventTypeProp) && eventTypeProp.ValueKind == JsonValueKind.String)
                {
                    var eventType = eventTypeProp.GetString();
                    if (string.IsNullOrWhiteSpace(eventType))
                    {
                        return Results.BadRequest("event_type cannot be empty");
                    }

                    var routingKey = $"event.{eventType}";
                    logger.LogInformation("Direct event received: event_type={EventType}, RemoteIp={RemoteIp}",
                        eventType, remoteIp);

                    // Schema validation: enforce required fields per event type
                    var validationError = ValidateEventPayload(eventType, root);
                    if (validationError != null)
                    {
                        logger.LogWarning(
                            "Invalid {EventType} payload from {RemoteIp}: {Error}",
                            eventType, remoteIp, validationError);
                        return Results.BadRequest(new { error = validationError });
                    }

                    // Publish the raw JSON payload as-is to RabbitMQ
                    await publisher.PublishAsync(root, routingKey, ct);

                    return Results.Accepted();
                }

                return await PublishLegacyTicketAsync(root, publisher, logger, remoteIp, ct);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Malformed JSON in ingestion request from {RemoteIp}", remoteIp);
                return Results.BadRequest("Invalid JSON format");
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to publish event for ingestion from {RemoteIp}. RabbitMQ may be unavailable.",
                    remoteIp);
                return Results.StatusCode(503); // Service Unavailable — caller can retry
            }
        }

        app.MapPost("/api/ingest", ProcessIngestionRequestAsync);
        app.MapPost("/ingest", ProcessIngestionRequestAsync);

        app.MapMetrics();

        app.Run();
    }

    /// <summary>
    /// Validates required fields for known event types.
    /// Returns a human-readable error message, or null if the payload is valid.
    /// Unknown event types are always accepted (forward-compatibility).
    /// </summary>
    private static string? ValidateEventPayload(string? eventType, JsonElement root)
    {
        return eventType switch
        {
            "ticket.created" => ValidateRequiredStringFields(root,
                "ticket_id", "customer_email", "customer_name"),
            "ticket.resolved" => ValidateRequiredStringFields(root,
                "ticket_id"),
            "invoice.created" => ValidateRequiredStringFields(root,
                "invoice_id", "customer_email"),
            _ => null // Unknown/future event types pass through
        };
    }

    private static string? ValidateRequiredStringFields(JsonElement root, params string[] requiredFields)
    {
        var missing = requiredFields
            .Where(field =>
                !root.TryGetProperty(field, out var prop) ||
                prop.ValueKind == JsonValueKind.Null ||
                (prop.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(prop.GetString())))
            .ToArray();

        return missing.Length > 0
            ? $"Missing required fields: {string.Join(", ", missing)}"
            : null;
    }

    private static string GetDataString(Dictionary<string, object> data, string fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = data.GetValueOrDefault(key)?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return fallback;
    }

    private static async Task<IResult> PublishLegacyTicketAsync(
        JsonElement root,
        RabbitMqConnector.IRabbitMqPublisher publisher,
        ILogger<Program> logger,
        System.Net.IPAddress? remoteIp,
        CancellationToken ct)
    {
        var request = JsonSerializer.Deserialize<IngestionRequest>(root.GetRawText(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (request == null)
        {
            logger.LogWarning("Null ingestion request from {RemoteIp}", remoteIp);
            return Results.BadRequest("Request body is required");
        }

        if (request.Data == null || request.Data.Count == 0)
        {
            logger.LogWarning("Ingestion request with empty data from {RemoteIp}", remoteIp);
            return Results.BadRequest("Data dictionary is required and cannot be empty");
        }

        var ticketId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow.ToString("O");

        var evt = new RabbitMqConnector.Contracts.TicketCreatedEvent
        {
            EventType = "ticket.created",
            Timestamp = now,
            Source = "gatekeeper-api",
            TicketId = ticketId,
            CustomerEmail = GetDataString(request.Data, "unknown@example.com", "email", "CustomerEmail"),
            CustomerName = GetDataString(request.Data, "External User", "name", "CustomerName"),
            TenantId = GetDataString(request.Data, string.Empty, "tenant_id", "TenantId"),
            Description = GetDataString(request.Data, $"New ticket via {request.Template}", "description", "body", "subject"),
            Priority = GetDataString(request.Data, "medium", "priority").ToLowerInvariant(),
            CreatedAt = now
        };

        await publisher.PublishAsync(evt, "event.ticket.created", ct);

        logger.LogInformation(
            "Published ticket.created event from IngestionRequest: TicketId={TicketId}, Template={Template}, RemoteIp={RemoteIp}",
            ticketId,
            request.Template,
            remoteIp);

        return Results.Accepted();
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


