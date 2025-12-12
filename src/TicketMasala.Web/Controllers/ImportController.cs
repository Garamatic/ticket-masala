using TicketMasala.Web.Models;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = "Admin,Employee")]
public class ImportController : Controller
{
    private readonly ITicketImportService _importService;
    private readonly ITicketService _ticketService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(ITicketImportService importService, ITicketService ticketService, ILogger<ImportController> logger)
    {
        _importService = importService;
        _ticketService = ticketService;
        _logger = logger;
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
            using var stream = System.IO.File.OpenRead(tempPath);
            var rows = _importService.ParseFile(stream, originalFileName);

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

            var count = await _importService.ImportTicketsAsync(rows, mapping, uploaderId, departmentId.Value);

            TempData["Success"] = $"Successfully imported {count} tickets.";

            // Cleanup
            System.IO.File.Delete(tempPath);

            return RedirectToAction("Index", "Ticket");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing import");
            TempData["Error"] = "Error executing import.";
            return RedirectToAction(nameof(Index));
        }
    }
}
