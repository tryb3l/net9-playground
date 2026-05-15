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

    public TagController(ITagService tagService, ILogger<TagController> logger)
    {
        _tagService = tagService;
        _logger = logger;
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

            tag.Name = viewModel.Name;
            await _tagService.UpdateTagAsync(tag);
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