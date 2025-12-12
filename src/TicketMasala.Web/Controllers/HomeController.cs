using System.Diagnostics;
using TicketMasala.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketMasala.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Get error details
        var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var errorMessage = "An unexpected error occurred.";
        var statusCode = 500;

        if (exceptionHandlerPathFeature?.Error != null)
        {
            var exception = exceptionHandlerPathFeature.Error;

            // Log the exception (if not already logged by middleware)
            _logger.LogError(exception, "Unhandled exception encountered");

            if (exception is UnauthorizedAccessException)
            {
                errorMessage = "You do not have permission to access this resource.";
                statusCode = 403;
            }
            else if (exception is KeyNotFoundException)
            {
                errorMessage = "The requested resource could not be found.";
                statusCode = 404;
            }
            else if (exception is OperationCanceledException)
            {
                errorMessage = "The operation was cancelled.";
                statusCode = 499; // Client Closed Request
            }
        }

        Response.StatusCode = statusCode;

        var viewModel = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
        };

        // Pass error message to view (using ViewData to avoid modifying ViewModel definition if possible, 
        // or we could extend the view model)
        ViewData["ErrorMessage"] = errorMessage;
        ViewData["StatusCode"] = statusCode;

        return View(viewModel);
    }
}
