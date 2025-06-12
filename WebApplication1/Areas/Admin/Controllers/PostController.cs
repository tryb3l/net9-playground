using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Areas.Admin.ViewModels.Post;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PostController : Controller
{
    private readonly IPostService _postService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<PostController> _logger;

    public PostController(IPostService postService, ICategoryService categoryService, ITagService tagService, UserManager<User> userManager, ILogger<PostController> logger)
    {
        _postService = postService;
        _categoryService = categoryService;
        _tagService = tagService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, string? searchTerm = null, string? tagFilter = null, bool? publishedOnly = null)
    {
        var viewModel = await _postService.GetPostListAsync(page, searchTerm, tagFilter, publishedOnly);
        return View("PostsList", viewModel);
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
            AvailableTags = await _tagService.GetAvailableTagsAsync(),
            AvailableCategories = await _categoryService.GetAvailableCategoriesAsync()
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
            if (currentUser == null)
            {
                ModelState.AddModelError("", "Unable to identify current user. Please log in again");
                viewModel.AvailableTags = await _tagService.GetAvailableTagsAsync();
                viewModel.AvailableCategories = await _categoryService.GetAvailableCategoriesAsync();
                return View(viewModel);
            }
            try
            {
                await _postService.CreatePostAsync(viewModel, currentUser.Id);
                TempData["SuccessMessage"] = "Post created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post for user {UserId}, ViewModel: {@ViewModel}", currentUser.Id, viewModel);
                ModelState.AddModelError("", $"An unexpected error occurred while creating the post. Please try again.");
            }
        }

        viewModel.AvailableTags = await _tagService.GetAvailableTagsAsync();
        viewModel.AvailableCategories = await _categoryService.GetAvailableCategoriesAsync();
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

        viewModel.AvailableTags = await _tagService.GetAvailableTagsAsync();
        viewModel.AvailableCategories = await _categoryService.GetAvailableCategoriesAsync();
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

        return Ok(viewModel);
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