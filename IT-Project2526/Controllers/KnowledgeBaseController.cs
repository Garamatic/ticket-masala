using IT_Project2526.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Controllers
{
    [Authorize]
    public class KnowledgeBaseController : Controller
    {
        private readonly ITProjectDB _context;
        private readonly ILogger<KnowledgeBaseController> _logger;

        public KnowledgeBaseController(ITProjectDB context, ILogger<KnowledgeBaseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: KnowledgeBase
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.KnowledgeBaseArticles
                .Include(a => a.Author)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(a => 
                    a.Title.ToLower().Contains(term) || 
                    a.Tags.ToLower().Contains(term) ||
                    a.Content.ToLower().Contains(term));
            }

            var articles = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
            ViewData["SearchTerm"] = searchTerm;
            return View(articles);
        }

        // GET: KnowledgeBase/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var article = await _context.KnowledgeBaseArticles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (article == null) return NotFound();

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
                
                _context.Add(article);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(article);
        }

        // GET: KnowledgeBase/Edit/5
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var article = await _context.KnowledgeBaseArticles.FindAsync(id);
            if (article == null) return NotFound();
            
            return View(article);
        }

        // POST: KnowledgeBase/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Content,Tags,CreatedAt,AuthorId")] KnowledgeBaseArticle article)
        {
            if (id != article.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    article.UpdatedAt = DateTime.UtcNow;
                    _context.Update(article);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArticleExists(article.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(article);
        }

        private bool ArticleExists(Guid id)
        {
            return _context.KnowledgeBaseArticles.Any(e => e.Id == id);
        }
    }
}
