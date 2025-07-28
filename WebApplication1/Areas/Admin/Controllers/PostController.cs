using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Areas.Admin.ViewModels.Post;
using WebApplication1.Helpers;
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
    private readonly IActivityLogService _activityLogService;


    public PostController(IPostService postService, ICategoryService categoryService, ITagService tagService, UserManager<User> userManager, ILogger<PostController> logger, IActivityLogService activityLogService)
    {
        _postService = postService;
        _categoryService = categoryService;
        _tagService = tagService;
        _userManager = userManager;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    public IActionResult Index()
    {
        return View("PostsList");
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

    [HttpGet]
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
            Debug.Assert(currentUser != null, nameof(currentUser) + " != null");
            var createdPost = await _postService.CreatePostAsync(viewModel, currentUser.Id);

            await _activityLogService.LogActivityAsync(currentUser.Id, "Created", "Post",
                $"Created new post: '{createdPost?.Title ?? "Untitled"}'");


            TempData["SuccessMessage"] = $"Post '{createdPost?.Title}' created successfully.";
            Debug.Assert(createdPost != null, nameof(createdPost) + " != null");
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

    [HttpGet]
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

        viewModel.PublishNow = viewModel.IsPublished;

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

        var isPublishAction = HttpContext.Request.Form["PublishNow"].Contains("true");

        var updatedViewModel = new EditPostViewModel
        {
            Id = viewModel.Id,
            Title = viewModel.Title,
            Content = viewModel.Content,
            CategoryId = viewModel.CategoryId,
            SelectedTagIds = viewModel.SelectedTagIds,
            IsPublished = isPublishAction,
            AvailableCategories = viewModel.AvailableCategories,
            AvailableTags = viewModel.AvailableTags
        };
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Edit POST - ModelState is invalid. Returning view with validation errors.");
            updatedViewModel.AvailableCategories = await _categoryService.GetAvailableCategoriesAsync();
            updatedViewModel.AvailableTags = await _tagService.GetAvailableTagsAsync();
            return View(updatedViewModel);
        }

        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            await _postService.UpdatePostAsync(id, updatedViewModel);

            if (currentUser != null)
            {
                await _activityLogService.LogActivityAsync(currentUser.Id, "Updated", "Post", $"Updated post: '{updatedViewModel.Title}'");
            }

            TempData["SuccessMessage"] = isPublishAction ? "Post has been published successfully." : "Draft has been saved successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating post with ID {PostId}", id);
            TempData["ErrorMessage"] = "An unexpected error occurred while saving the post.";

            updatedViewModel.AvailableCategories = await _categoryService.GetAvailableCategoriesAsync();
            updatedViewModel.AvailableTags = await _tagService.GetAvailableTagsAsync();
            return View(updatedViewModel);
        }
    }


    [HttpGet]
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
    public async Task<IActionResult> GetPostsData([FromForm] DataTablesRequest request)
    {
        var pagedData = await _postService.GetPostListForDataTableAsync(request);
        return Json(pagedData);
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
                return Json(new { success = false, message = "Post not found." });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            await _postService.SoftDeletePostAsync(id);
            Debug.Assert(currentUser != null, nameof(currentUser) + " != null");
            await _activityLogService.LogActivityAsync(currentUser.Id, "Trashed", "Post", $"Moved post to trash: '{post.Title}'");


            return Json(new { success = true, message = $"Post '{post.Title}' moved to trash successfully." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Post not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting post with ID {PostId}", id);
            return StatusCode(500, new { message = "Error moving post to trash. Please try again." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        try
        {
            var post = await _postService.GetPostByIdAsync(id, includeUnpublished: true, includeDeleted: true);
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found." });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            await _postService.RestorePostAsync(id);
            Debug.Assert(currentUser != null, nameof(currentUser) + " != null");
            await _activityLogService.LogActivityAsync(currentUser.Id, "Restored", "Post", $"Restored post: '{post.Title}'");

            return Json(new { success = true, message = $"Post '{post.Title}' restored successfully." });
        }
        catch (KeyNotFoundException)
        {
            return Json(new { success = false, message = "Post not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring post with ID {PostId}", id);
            return Json(new { success = false, message = "Error restoring post. Please try again." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        try
        {
            var post = await _postService.GetPostByIdAsync(id, includeUnpublished: true);
            if (post == null) return Json(new { success = false, message = "Post not found." });

            var currentUser = await _userManager.GetUserAsync(User);
            await _postService.PublishPostAsync(id);
            Debug.Assert(currentUser != null, nameof(currentUser) + " != null");
            await _activityLogService.LogActivityAsync(currentUser.Id, "Published", "Post",
                $"Published post: '{post.Title ?? "Untitled"}'");

            // Return JSON for AJAX requests
            return Json(new { success = true, message = "Post published successfully." });
        }
        catch (KeyNotFoundException)
        {
            return Json(new { success = false, message = "Post not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing post with ID {PostId}", id);
            return Json(new { success = false, message = "An error occurred while publishing post." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id)
    {
        try
        {
            var post = await _postService.GetPostByIdAsync(id, includeUnpublished: true);
            if (post == null) return Json(new { success = false, message = "Post not found." });

            var currentUser = await _userManager.GetUserAsync(User);
            await _postService.UnpublishPostAsync(id);
            if (currentUser != null)
                await _activityLogService.LogActivityAsync(currentUser.Id, "Unpublished", "Post",
                    $"Unpublished post: '{post.Title ?? "Untitled"}'");

            // Return JSON for AJAX requests
            return Json(new { success = true, message = "Post unpublished successfully." });
        }
        catch (KeyNotFoundException)
        {
            return Json(new { success = false, message = "Post not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing post with ID {PostId}", id);
            return Json(new { success = false, message = "An error occurred while unpublishing post." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmptyTrash()
    {
        try
        {
            await _postService.EmptyTrashAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emptying trash");
            return Json(new { success = false, message = "An error occurred while emptying trash." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreAll()
    {
        try
        {
            await _postService.RestoreAllPostsAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring all posts");
            return Json(new { success = false, message = "An error occurred while restoring posts." });
        }
    }
}