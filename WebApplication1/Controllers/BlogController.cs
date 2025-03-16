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

    public async Task<ActionResult> Index(int page = 1, string? category = null, string? tag = null)
    {
        var viewModel = await _postService.GetBlogIndexViewModelAsync(page, category, tag);
        return View(viewModel);
    }

    public async Task<IActionResult> Post(int id, string? slug)
    {
        var post = await _postService.GetPostByIdAsync(id);

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
