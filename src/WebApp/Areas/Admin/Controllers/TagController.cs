using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels.Tag;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TagController : Controller
{
    private readonly ITagService _tagService;
    private readonly ILogger<TagController> _logger;
    private readonly IActivityLogService _activityLogService;

    public TagController(
        ITagService tagService,
        ILogger<TagController> logger,
        IActivityLogService activityLogService)
    {
        _tagService = tagService;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var tags = await _tagService.GetAllTagsWithCountAsync();
        return View(tags);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateTagViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTagViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Create tag request failed validation.");
            return View(viewModel);
        }

        try
        {
            var tag = new Tag { Name = viewModel.Name.Trim() };
            await _tagService.CreateTagAsync(tag);
            _logger.LogInformation("Tag created successfully: {TagName}", tag.Name);
            await _activityLogService.LogActivityAsync("Created", "Tag", $"Created tag: '{tag.Name}'");
            TempData["SuccessMessage"] = $"Tag '{tag.Name}' created successfully";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag");
            TempData["ErrorMessage"] = "Error creating tag. Please try again.";
            return View(viewModel);
        }
    }

    /// <summary>
    /// Creates a tag from inside the post editor so writing a post never requires navigating
    /// away (and losing the draft) just to add a tag.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreate(string? name)
    {
        var trimmed = name?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return Json(new { success = false, message = "Tag name is required." });
        }

        if (trimmed.Length > 64)
        {
            return Json(new { success = false, message = "Tag name must be 64 characters or fewer." });
        }

        try
        {
            var (tag, created) = await _tagService.GetOrCreateTagAsync(trimmed);
            if (created)
            {
                await _activityLogService.LogActivityAsync("Created", "Tag", $"Created tag: '{tag.Name}'");
            }

            return Json(new { success = true, id = tag.Id, name = tag.Name, created });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error quick-creating tag {TagName}", trimmed);
            return Json(new { success = false, message = "Could not add the tag. Please try again." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var tag = await _tagService.GetTagByIdAsync(id.Value);
        if (tag == null) return NotFound();

        return View(new EditTagViewModel { Id = tag.Id, Name = tag.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditTagViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();
        if (!ModelState.IsValid) return View(viewModel);

        try
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null) return NotFound();

            var originalName = tag.Name;
            tag.Name = viewModel.Name;
            await _tagService.UpdateTagAsync(tag);
            await _activityLogService.LogActivityAsync("Updated", "Tag", $"Renamed tag '{originalName}' to '{tag.Name}'");
            TempData["SuccessMessage"] = "Tag updated successfully";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag");
            TempData["ErrorMessage"] = "Error updating tag";
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var tag = await _tagService.GetTagByIdAsync(id.Value);
        if (tag != null) return View(tag);
        TempData["ErrorMessage"] = "Tag not found";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Attempting to delete tag with ID: {TagId}", id);

        try
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null)
            {
                _logger.LogWarning("Tag with ID {TagId} not found for deletion", id);
                TempData["ErrorMessage"] = "Tag not found";
                return RedirectToAction(nameof(Index));
            }

            await _tagService.DeleteTagAsync(id);
            _logger.LogInformation("Tag '{TagName}' (ID: {TagId}) deleted successfully", tag.Name, id);
            await _activityLogService.LogActivityAsync("Deleted", "Tag", $"Deleted tag: '{tag.Name}'");
            TempData["SuccessMessage"] = $"Tag '{tag.Name}' deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag with ID {TagId}", id);
            TempData["ErrorMessage"] = "Error deleting tag";
        }

        return RedirectToAction(nameof(Index));
    }
}