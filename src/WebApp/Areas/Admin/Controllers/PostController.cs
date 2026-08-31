using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Helpers;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

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


    public PostController(IPostService postService, 
        ICategoryService categoryService, 
        ITagService tagService, 
        UserManager<User> userManager, 
        ILogger<PostController> logger, 
        IActivityLogService activityLogService)
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
            foreach (var error in ViewData.ModelState.Values.SelectMany(modelState => modelState.Errors))
            {
                _logger.LogWarning("Validation Error: {ErrorMessage}", error.ErrorMessage);
            }

            viewModel.AvailableTags = await _tagService.GetAvailableTagsAsync();
            viewModel.AvailableCategories = await _categoryService.GetAvailableCategoriesAsync();
            return View(viewModel);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            _logger.LogError("Current user not found during post creation");
            return Unauthorized();
        }

        try
        {
            var createdPost = await _postService.CreatePostAsync(viewModel, currentUser.Id);
            await _activityLogService.LogActivityAsync("Created", "Post", $"Created new post: '{createdPost.Title}'");

            TempData["SuccessMessage"] = $"Post '{createdPost.Title}' created successfully.";
            _logger.LogInformation("Post created successfully with ID {PostId}", createdPost.Id);
            // UX Improvement: Redirect to Edit instead of Index so user can continue working
            return RedirectToAction(nameof(Edit), new { id = createdPost.Id });
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

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Edit POST - ModelState is invalid. Returning view with validation errors.");
            viewModel.AvailableCategories = await _categoryService.GetAvailableCategoriesAsync();
            viewModel.AvailableTags = await _tagService.GetAvailableTagsAsync();
            return View(viewModel);
        }

        try
        {
            await _postService.UpdatePostAsync(id, viewModel);
            await _activityLogService.LogActivityAsync("Updated", "Post", $"Updated post: '{viewModel.Title}'");

            TempData["SuccessMessage"] = viewModel.PublishNow ? "Post has been published successfully." : "Draft has been saved successfully.";
            // UX Improvement: Stay on the Edit page
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating post with ID {PostId}", id);
            TempData["ErrorMessage"] = "An unexpected error occurred while saving the post.";

            viewModel.AvailableCategories = await _categoryService.GetAvailableCategoriesAsync();
            viewModel.AvailableTags = await _tagService.GetAvailableTagsAsync();
            return View(viewModel);
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            // Captured before the delete so the audit entry can name the post.
            var post = await _postService.GetPostByIdAsync(id, includeUnpublished: true, includeDeleted: true);
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found." });
            }

            await _postService.DeletePostAsync(id);
            await _activityLogService.LogActivityAsync("Deleted", "Post", $"Permanently deleted post: '{post.Title}'");

            return Json(new { success = true, message = "Post deleted permanently." });
        }
        catch (KeyNotFoundException)
        {
            return Json(new { success = false, message = "Post not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post with ID {PostId}", id);
            return Json(new { success = false, message = "Error deleting post. Please try again." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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

            await _postService.SoftDeletePostAsync(id);
            await _activityLogService.LogActivityAsync("Trashed", "Post", $"Moved post to trash: '{post.Title}'");

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

            await _postService.RestorePostAsync(id);
            await _activityLogService.LogActivityAsync("Restored", "Post", $"Restored post: '{post.Title}'");

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

            await _postService.PublishPostAsync(id);
            await _activityLogService.LogActivityAsync("Published", "Post", $"Published post: '{post.Title}'");

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

            await _postService.UnpublishPostAsync(id);
            await _activityLogService.LogActivityAsync("Unpublished", "Post", $"Unpublished post: '{post.Title}'");
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
            var deleted = await _postService.EmptyTrashAsync();
            await _activityLogService.LogActivityAsync("Deleted", "Post",
                $"Emptied trash, permanently deleting {deleted} post(s)");

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
            var restored = await _postService.RestoreAllPostsAsync();
            await _activityLogService.LogActivityAsync("Restored", "Post",
                $"Restored {restored} post(s) from trash");

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring all posts");
            return Json(new { success = false, message = "An error occurred while restoring posts." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CleanupImages()
    {
        try
        {
            var deleted = await _postService.CleanupOrphanedEditorImagesAsync();
            if (deleted > 0)
            {
                await _activityLogService.LogActivityAsync("Deleted", "Attachment",
                    $"Removed {deleted} unreferenced editor image(s)");
            }

            return Json(new
            {
                success = true,
                message = deleted == 0
                    ? "No unused images found."
                    : $"Removed {deleted} unused image(s)."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned editor images");
            return Json(new { success = false, message = "An error occurred while cleaning up images." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var viewModel = await _postService.GetPostViewModelAsync(id);
        if (viewModel == null)
            return NotFound();

        ViewData["IsPreview"] = true;
        return View("~/Views/Blog/Post.cshtml", viewModel);
    }
}