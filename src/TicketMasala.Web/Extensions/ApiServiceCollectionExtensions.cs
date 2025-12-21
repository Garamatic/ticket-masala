using Microsoft.AspNetCore.Mvc.Razor;

namespace TicketMasala.Web.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddMasalaApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Ticket Masala API",
                Version = "v1",
                Description = "Configuration-driven work management API. Valid DomainId values are sourced from masala_domains.yaml configuration."
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        services.AddControllersWithViews()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();

        services.AddRazorPages();

        return services;
    }
}
