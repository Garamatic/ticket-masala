using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using TicketMasala.Web.Engine.Ingestion.Background;

namespace GatekeeperApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register the ingestion queue and worker
            builder.Services.AddSingleton<IngestionQueue<string>>();
            builder.Services.AddHostedService<IngestionWorker>();

            var app = builder.Build();

            app.MapPost("/api/ingest", async (HttpContext context, IngestionQueue<string> queue) =>
            {
                using var reader = new System.IO.StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                await queue.EnqueueAsync(body);
                return Results.Accepted();
            });

            app.Run();
        }
    }
}
