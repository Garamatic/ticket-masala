using WebOptimizer;
using Microsoft.AspNetCore.Mvc.Razor;

namespace TicketMasala.Web.Extensions;

public static class FrontendServiceCollectionExtensions
{
    public static IServiceCollection AddMasalaFrontend(this IServiceCollection services)
    {
        services.AddLocalization();

        services.AddWebOptimizer(pipeline =>
        {
            pipeline.AddCssBundle("/css/bundle.css",
                "lib/bootstrap/dist/css/bootstrap.min.css",
                "css/design-system.css",
                "css/site.css")
                .MinifyCss();

            pipeline.AddJavaScriptBundle("/js/bundle.js",
                "lib/jquery/dist/jquery.min.js",
                "lib/bootstrap/dist/js/bootstrap.bundle.min.js",
                "js/site.js",
                "js/toast.js")
                .MinifyJavaScript();
        });

        return services;
    }
}
