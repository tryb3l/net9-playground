using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BlogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index(int page = 1, string? category = null, string? tag = null)
        {
            int pageSize = 5;
            var postQuery = _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                postQuery = postQuery.Where(p => p.Category != null && p.Category.Slug == category);
            }

            if (!string.IsNullOrEmpty(tag))
            {
                postQuery = postQuery.Where(p => p.PostTags.Any(pt => pt.Tag != null && pt.Tag.Name == tag));
            }

            var totalPosts = await postQuery.CountAsync();

            var posts = await postQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            var tags = await _context.Tags
                .OrderByDescending(t => t.PostTags.Count)
                .Take(10)
                .ToListAsync();

            var viewModel = new BlogIndexViewModel
            {
                Posts = posts,
                Categories = categories,
                Tags = tags,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
                CurrentCategory = category,
                CurrentTag = tag
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Post(int id, string? slug)
        {
            var post = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsPublished);

            if (post == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(post.Slug) && slug != post.Slug)
            {
                return RedirectToAction(nameof(Post), new { id = post.Id, slug = post.Slug });
            }

            return View(post);
        }

    }
}
