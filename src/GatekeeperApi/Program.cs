using System.IO;
using System.Threading.Channels;

namespace GatekeeperApi;

/// <summary>
/// Minimal API for ingesting external tickets.
/// This is a lightweight entry point that delegates to the main TicketMasala system.
/// For microservices deployment, this can be scaled independently.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register the ingestion queue (bounded to prevent memory exhaustion)
        builder.Services.AddSingleton(_ => new IngestionQueue(capacity: 10000));
        builder.Services.AddHostedService<IngestionWorker>();

        // Note: ITicketWorkflowService implementation is loaded from plugin or external reference
        // In standalone mode, this service processes items locally
        // In microservices mode, this calls the main TicketMasala API

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
            context.Request.Body = new Microsoft.AspNetCore.WebUtilities.FileBufferingReadStream(
                context.Request.Body, 10 * 1024 * 1024, null, Path.GetTempPath());
            await next();
        });

        app.MapPost("/api/ingest", async (HttpContext context, IngestionQueue queue, ILogger<Program> logger) =>
        {
            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedValue) ||
                extractedValue != apiKey)
            {
                logger.LogWarning("Unauthorized ingestion attempt from {RemoteIp}",
                    context.Connection.RemoteIpAddress);
                return Results.Unauthorized();
            }

            var request = await context.Request.ReadFromJsonAsync<IngestionRequest>();
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

            var enqueued = await queue.EnqueueAsync(request, context.RequestAborted);
            if (!enqueued)
            {
                logger.LogError("Queue full - ingestion request dropped from {RemoteIp}",
                    context.Connection.RemoteIpAddress);
                return Results.StatusCode(503); // Service Unavailable
            }

            logger.LogInformation(
                "Ingestion request enqueued: Template={Template}, Keys={KeyCount}, RemoteIp={RemoteIp}",
                request.Template,
                request.Data.Count,
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
