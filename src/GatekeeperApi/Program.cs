using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Collections.Generic;
using TicketMasala.Web.Extensions;

namespace GatekeeperApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register the ingestion queue and worker
            builder.Services.AddSingleton<IngestionQueue<IngestionRequest>>();
            builder.Services.AddHostedService<IngestionWorker>();

            // Register TicketMasala services needed for ingestion
            // We use the same extensions as the Web app to ensure consistency
            builder.Services.AddMasalaDatabase(builder.Configuration, builder.Environment);
            builder.Services.AddRepositories();
            builder.Services.AddObservers();
            builder.Services.AddCoreServices();
            builder.Services.AddGerdaServices(builder.Environment,
                TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigBasePath(builder.Environment.ContentRootPath));

            builder.Services.AddScoped<TicketMasala.Web.Engine.Ingestion.IIngestionTemplateService,
                TicketMasala.Web.Engine.Ingestion.IngestionTemplateService>();

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

    public class IngestionRequest
    {
        public string Template { get; set; } = "default";
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
