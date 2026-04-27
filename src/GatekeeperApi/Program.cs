using System.IO;

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

        builder.Services.AddSingleton(_ => new IngestionQueue(capacity: 10000));
        builder.Services.AddHostedService<IngestionWorker>();

        var app = builder.Build();

        var apiKey = builder.Configuration["Gatekeeper:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "Gatekeeper:ApiKey must be configured. Set it via environment variable or appsettings.json");
        }

        // Configure request size limit (10MB to prevent abuse)
        // FileBufferingReadStream buffers large requests to disk instead of memory
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

        app.MapPost("/api/ingest", async (HttpContext context, IngestionQueue queue, ILogger<Program> logger) =>
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
                request = await context.Request.ReadFromJsonAsync<IngestionRequest>();
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

            var enqueued = queue.TryEnqueue(request);
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
