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
        try
        {
            var viewModel = new CreatePostViewModel
            {
                AvailableTags = await _tagService.GetAvailableTagsAsync(),
                AvailableCategories = await _categoryService.GetAvailableCategoriesAsync()
            };

            _logger.LogInformation("Create GET - Loaded {TagCount} tags and {CategoryCount} categories",
                viewModel.AvailableTags.Count, viewModel.AvailableCategories.Count);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create post page");
            TempData["ErrorMessage"] = "Error loading the create post page. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePostViewModel viewModel)
    {
        _logger.LogInformation("Create POST action initiated.");
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid. Returning view with validation errors.");
            foreach (var modelState in ViewData.ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    _logger.LogWarning("Validation Error: {ErrorMessage}", error.ErrorMessage);
                }
            }
            
            viewModel.AvailableTags = await _tagService.GetAvailableTagsAsync();
            viewModel.AvailableCategories = await _categoryService.GetAvailableCategoriesAsync();
            return View(viewModel);
        }

        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var createdPost = await _postService.CreatePostAsync(viewModel, currentUser.Id);

            TempData["SuccessMessage"] = $"Post '{createdPost.Title}' created successfully.";
            _logger.LogInformation("Post created successfully with ID {PostId}", createdPost.Id);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the post.");
            TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
            
            viewModel.AvailableTags = await _tagService.GetAvailableTagsAsync();
            viewModel.AvailableCategories = await _categoryService.GetAvailableCategoriesAsync();
            return View(viewModel);
        }
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
                TempData["SuccessMessage"] = "Post updated successfully.";
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
        try
        {
            await _postService.DeletePostAsync(id);
            TempData["SuccessMessage"] = "Post deleted successfully.";
        }
        catch (KeyNotFoundException)
        {
            TempData["ErrorMessage"] = "Post not found.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post with ID {PostId}", id);
            TempData["ErrorMessage"] = "Error deleting post. Please try again.";
        }
        return RedirectToAction(nameof(Index));
    }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SoftDelete(int id)
{
    try
    {
        var post = await _postService.GetPostByIdAsync(id, includeUnpublished: true);
        if (post == null)
        {
            TempData["ErrorMessage"] = "Post not found.";
            return RedirectToAction(nameof(Index));
        }
        
        var postTitle = post.Title;
        
        await _postService.SoftDeletePostAsync(id);

        TempData["SuccessMessage"] = $"Post '{postTitle}' moved to trash successfully.";
        _logger.LogInformation("Post '{PostTitle}' (ID: {PostId}) soft deleted successfully", postTitle, id);
    }
    catch (KeyNotFoundException)
    {
        TempData["ErrorMessage"] = "Post not found.";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error soft deleting post with ID {PostId}", id);
        TempData["ErrorMessage"] = "Error moving post to trash. Please try again.";
    }
    return RedirectToAction(nameof(Index));
}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        try
        {
            await _postService.RestorePostAsync(id);
            TempData["SuccessMessage"] = "Post restored successfully.";
        }
        catch (KeyNotFoundException)
        {
            TempData["ErrorMessage"] = "Post not found.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring post with ID {PostId}", id);
            TempData["ErrorMessage"] = "Error restoring post. Please try again.";
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
            TempData["SuccessMessage"] = "Post published successfully.";
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
            TempData["SuccessMessage"] = "Post unpublished successfully.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Index));
    }
}