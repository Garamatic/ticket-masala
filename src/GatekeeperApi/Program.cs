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

        // Register the ingestion queue and worker
        builder.Services.AddSingleton<IngestionQueue<IngestionRequest>>();
        builder.Services.AddHostedService<IngestionWorker>();

        // Note: ITicketWorkflowService implementation is loaded from plugin or external reference
        // In standalone mode, this service processes items locally
        // In microservices mode, this calls the main TicketMasala API

        var app = builder.Build();

        var apiKey = builder.Configuration["Gatekeeper:ApiKey"] ?? "masala-secret-key";

        app.MapPost("/api/ingest", async (HttpContext context, IngestionQueue<IngestionRequest> queue) =>
        {
            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedValue) ||
                extractedValue != apiKey)
            {
                return Results.Unauthorized();
            }

            var request = await context.Request.ReadFromJsonAsync<IngestionRequest>();
            if (request == null || request.Data == null)
            {
                return Results.BadRequest("Invalid payload");
            }

            await queue.EnqueueAsync(request);
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
