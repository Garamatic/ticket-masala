using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = "Admin,Employee")]
public class ImportController : Controller
{
    private readonly ITicketImportService _importService;
    private readonly ITicketService _ticketService;
    private readonly ILogger<ImportController> _logger;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceScopeFactory _scopeFactory;

    public ImportController(
        ITicketImportService importService,
        ITicketService ticketService,
        ILogger<ImportController> logger,
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory scopeFactory)
    {
        _importService = importService;
        _ticketService = ticketService;
        _logger = logger;
        _taskQueue = taskQueue;
        _scopeFactory = scopeFactory;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Parse file to get headers and sample data
            using var stream = file.OpenReadStream();
            var rows = _importService.ParseFile(stream, file.FileName);

            if (rows.Count == 0)
            {
                TempData["Error"] = "File is empty.";
                return RedirectToAction(nameof(Index));
            }

            // Store rows in TempData/Session for next steps (simplified for pilot)
            // In production, might save to temp file or DB
            var json = JsonConvert.SerializeObject(rows.Take(5).ToList()); // Store sample
            TempData["CsvSample"] = json;

            // Store full data in session or cache? For pilot, maybe just re-upload or keep in memory if small
            // Better approach for pilot: Save temp file
            var tempPath = Path.GetTempFileName();
            using (var fileStream = new FileStream(tempPath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }
            TempData["TempFilePath"] = tempPath;
            TempData["OriginalFileName"] = file.FileName; // Store original filename to preserve extension

            // Get headers from first row
            var firstRow = (IDictionary<string, object>)rows.First();
            var headers = firstRow.Keys.ToList();

            return View("MapFields", headers);
        }
        catch (NotSupportedException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing file");
            TempData["Error"] = "Error parsing file.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> ExecuteImport(Dictionary<string, string> mapping)
    {
        var tempPath = TempData["TempFilePath"]?.ToString();
        var originalFileName = TempData["OriginalFileName"]?.ToString() ?? "temp.csv"; // Default to csv if missing

        if (mapping == null || !mapping.Any())
        {
            TempData["Error"] = "No field mapping provided.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrEmpty(tempPath) || !System.IO.File.Exists(tempPath))
        {
            TempData["Error"] = "Session expired. Please upload again.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var uploaderId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var departmentId = await _ticketService.GetCurrentUserDepartmentIdAsync();

            if (string.IsNullOrEmpty(uploaderId))
            {
                TempData["Error"] = "User not authenticated.";
                return RedirectToAction(nameof(Index));
            }

            if (departmentId == null || departmentId == Guid.Empty)
            {
                TempData["Error"] = "User has no department.";
                return RedirectToAction(nameof(Index));
            }

            var deptIdValue = departmentId.Value;

            // Enqueue Background Job
            await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
            {
                using var scope = _scopeFactory.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ImportController>>();
                
                try 
                {
                    logger.LogInformation("Starting background import for {FileName}", originalFileName);
                    var importService = scope.ServiceProvider.GetRequiredService<ITicketImportService>();
                    
                    using var stream = System.IO.File.OpenRead(tempPath);
                    var rows = importService.ParseFile(stream, originalFileName);
                    
                    var count = await importService.ImportTicketsAsync(rows, mapping, uploaderId, deptIdValue);
                    logger.LogInformation("Background import completed. Imported {Count} tickets.", count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during background import of {FileName}", originalFileName);
                }
                finally
                {
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
            });

            TempData["Success"] = "Import started in background. Please check the dashboard later.";
            return RedirectToAction("Index", "Ticket");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing import");
            TempData["Error"] = "Error starting import.";
            return RedirectToAction(nameof(Index));
        }
    }
}
