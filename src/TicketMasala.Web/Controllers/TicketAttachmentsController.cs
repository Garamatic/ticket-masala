using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.Core;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class TicketAttachmentsController : Controller
{
    private readonly IFileStorageService _fileStorage;
    private readonly MasalaDbContext _context;
    private readonly ILogger<TicketAttachmentsController> _logger;
    private readonly ISystemClock _clock;

    public TicketAttachmentsController(
        IFileStorageService fileStorage,
        MasalaDbContext context,
        ILogger<TicketAttachmentsController> logger,
        ISystemClock clock)
    {
        _fileStorage = fileStorage;
        _context = context;
        _logger = logger;
        _clock = clock;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(Guid ticketId, IFormFile file, bool isPublic)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            return RedirectToAction("Detail", "Ticket", new { id = ticketId });
        }

        try
        {
            var storedFileName = await _fileStorage.StoreFileAsync(file.OpenReadStream(), file.FileName);

            var document = new Document
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FileName = file.FileName,
                StoredFileName = storedFileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadDate = _clock.UtcNow,
                UploaderId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                IsPublic = isPublic
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            TempData["Success"] = "File uploaded successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for ticket {TicketId}", ticketId);
            TempData["Error"] = "An error occurred while uploading the file.";
        }

        return RedirectToAction("Detail", "Ticket", new { id = ticketId });
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid id)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc == null)
            return NotFound();

        try
        {
            var stream = await _fileStorage.RetrieveFileAsync(doc.StoredFileName);
            return File(stream, doc.ContentType, doc.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Preview(Guid id)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc == null)
            return NotFound();

        try
        {
            var stream = await _fileStorage.RetrieveFileAsync(doc.StoredFileName);
            // Return inline for preview
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{doc.FileName}\"");
            return File(stream, doc.ContentType);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}
