using Microsoft.AspNetCore.Mvc;
using WebApplication1.Interfaces;

namespace WebApplication1.Controllers;

public class BlogController : Controller
{
    private readonly IPostService _postService;

    public BlogController(IPostService postService)
    {
        _postService = postService;
    }

    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "page", "category", "tag" }, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult> Index(int page = 1, string? category = null, string? tag = null)
    {
        var viewModel = await _postService.GetBlogIndexViewModelAsync(page, category, tag);
        return View(viewModel);
    }

    [HttpGet("blog/{slug}")]
    [ResponseCache(Duration = 300, VaryByHeader = "User-Agent", Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Post(string slug)
    {
        var post = await _postService.GetPostBySlugAsync(slug);

        if (post == null)
        {
            return NotFound();
        }

        return View(post);
    }
}
