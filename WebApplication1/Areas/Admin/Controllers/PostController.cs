using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Areas.Admin.ViewModels.Posts;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PostController : Controller
{
    private readonly IPostService _postService;
    private readonly UserManager<User> _userManager;

    public PostController(IPostService postService, UserManager<User> userManager)
    {
        _postService = postService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int page = 1,
        string? searchTerm = null, string? tagFilter = null,
        bool? publishedOnly = null)
    {
        var viewModel = await _postService.GetPostListAsync(page, searchTerm, tagFilter, publishedOnly);
        return View(viewModel);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var post = await _postService.GetPostByIdAsync(id.Value, includeUnpublished: true);
        if (post == null)
        {
            return NotFound();
        }

        var viewModel = await _postService.GetPostViewModelAsync(id.Value);
        if (viewModel == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    public async Task<IActionResult> Create()
    {
        var viewModel = new CreatePostViewModel
        {
            AvailableTags = await _postService.GetAvailableTagsAsync()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePostViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            await _postService.CreatePostAsync(viewModel, currentUser?.Id ?? string.Empty);
            return RedirectToAction(nameof(Index));
        }
        viewModel.AvailableTags = await _postService.GetAvailableTagsAsync();
        return View(viewModel);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var viewModel = await _postService.GetPostForEditAsync(id.Value);
        if (viewModel == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditPostViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _postService.UpdatePostAsync(id, viewModel);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }

        viewModel.AvailableTags = await _postService.GetAvailableTagsAsync();
        return View(viewModel);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var viewModel = await _postService.GetPostViewModelAsync(id.Value);
        if (viewModel == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _postService.DeletePostAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoftDelete(int id)
    {
        try
        {
            await _postService.SoftDeletePostAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        try
        {
            await _postService.PublishPostAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id)
    {
        try
        {
            await _postService.UnpublishPostAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Index));
    }
}