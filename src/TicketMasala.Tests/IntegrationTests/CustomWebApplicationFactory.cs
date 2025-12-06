using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketMasala.Web;
using TicketMasala.Web.Data;

namespace TicketMasala.Tests.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Remove the app's DbContext registration.
            // Remove the app's DbContext registration.
            var descriptors = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<MasalaDbContext>) ||
                     d.ServiceType == typeof(DbContextOptions)).ToList();

            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(MasalaDbContext));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Add DbContext using an in-memory database for testing.
            services.AddDbContext<MasalaDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Build the service provider.
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context (MasalaDbContext).
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<MasalaDbContext>();
                var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory>>();

                // Ensure the database is created.
                db.Database.EnsureCreated();

                try
                {
                    // Seed the database with test data.
                    // Utilities.InitializeDbForTests(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the " +
                                        "database with test messages. Error: {Message}", ex.Message);
                }
            }
        });
    }
}
