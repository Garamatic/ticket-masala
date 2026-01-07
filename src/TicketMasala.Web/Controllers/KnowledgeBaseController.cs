using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class KnowledgeBaseController : Controller
{
    private readonly IKnowledgeBaseRepository _repository;
    private readonly ILogger<KnowledgeBaseController> _logger;

    public KnowledgeBaseController(IKnowledgeBaseRepository repository, ILogger<KnowledgeBaseController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // GET: KnowledgeBase
    public async Task<IActionResult> Index(string searchTerm)
    {
        IEnumerable<KnowledgeBaseArticle> articles;
        
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            articles = await _repository.GetAllAsync();
        }
        else
        {
            articles = await _repository.SearchAsync(searchTerm);
        }

        ViewData["SearchTerm"] = searchTerm;
        return View(articles);
    }

    // GET: KnowledgeBase/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();

        var article = await _repository.GetByIdAsync(id.Value);
        if (article == null) return NotFound();

        // Increment MasalaRank Usage Count
        await _repository.IncrementUsageCountAsync(id.Value);

        return View(article);
    }

    // GET: KnowledgeBase/Create
    [Authorize(Roles = "Admin,Employee")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: KnowledgeBase/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Create([Bind("Title,Content,Tags")] KnowledgeBaseArticle article)
    {
        if (ModelState.IsValid)
        {
            article.Id = Guid.NewGuid();
            article.CreatedAt = DateTime.UtcNow;
            article.AuthorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            // Set default MasalaRank values
            article.UsageCount = 0;
            article.IsVerified = false;

            await _repository.AddAsync(article);
            return RedirectToAction(nameof(Index));
        }
        return View(article);
    }

    // GET: KnowledgeBase/Edit/5
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var article = await _repository.GetByIdAsync(id.Value);
        if (article == null) return NotFound();

        return View(article);
    }

    // POST: KnowledgeBase/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Content,Tags,CreatedAt,AuthorId,UsageCount,IsVerified")] KnowledgeBaseArticle article)
    {
        if (id != article.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                article.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(article);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _repository.ExistsAsync(article.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(article);
    }
}
