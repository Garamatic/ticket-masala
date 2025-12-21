using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities; // ApplicationUser, Employee
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class TicketAttachmentsController : Controller
{
    private readonly IFileService _fileService;
    private readonly MasalaDbContext _context;
    private readonly ILogger<TicketAttachmentsController> _logger;

    public TicketAttachmentsController(
        IFileService fileService,
        MasalaDbContext context,
        ILogger<TicketAttachmentsController> logger)
    {
        _fileService = fileService;
        _context = context;
        _logger = logger;
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
            var storedFileName = await _fileService.SaveFileAsync(file, "tickets");

            var document = new Document
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FileName = file.FileName,
                StoredFileName = storedFileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadDate = DateTime.UtcNow,
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
        if (doc == null) return NotFound();

        var stream = await _fileService.GetFileStreamAsync(doc.StoredFileName, "tickets");
        if (stream == null) return NotFound();

        return File(stream, doc.ContentType, doc.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> Preview(Guid id)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc == null) return NotFound();

        var stream = await _fileService.GetFileStreamAsync(doc.StoredFileName, "tickets");
        if (stream == null) return NotFound();

        // Return inline for preview
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{doc.FileName}\"");
        return File(stream, doc.ContentType);
    }
}
