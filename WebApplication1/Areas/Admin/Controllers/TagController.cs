using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Areas.Admin.ViewModels.Tag;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TagController : Controller
{
    private readonly ITagService _tagService;

    public TagController(ITagService tagService)
    {
        _tagService = tagService;
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
        if (ModelState.IsValid)
        {
            try
            {
                var tag = new Tag { Name = viewModel.Name };
                await _tagService.CreateTagAsync(tag);
                TempData["SuccessMessage"] = $"Tag '{viewModel.Name}' has been created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the tag. Please try again.";
                Console.WriteLine($"Error creating tag: {ex.Message}");
            }
        }
        else
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"Model error: {error.ErrorMessage}");
            }
        }
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tag = await _tagService.GetTagByIdAsync(id.Value);
        if (tag == null)
        {
            return NotFound();
        }

        return View(new EditTagViewModel
        {
            Id = tag.Id,
            Name = tag.Name
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditTagViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid) return View(viewModel);
        try
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            tag.Name = viewModel.Name;
            await _tagService.UpdateTagAsync(tag);
            TempData["SuccessMessage"] = $"Tag '{viewModel.Name}' has been updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            if (!await _tagService.TagExistsAsync(viewModel.Id))
            {
                return NotFound();
            }
            TempData["ErrorMessage"] = "An error occurred while updating the tag. Please try again.";
        }
        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null)
            {
                TempData["ErrorMessage"] = "Tag not found.";
                return RedirectToAction(nameof(Index));
            }

            await _tagService.DeleteTagAsync(id);
            TempData["SuccessMessage"] = $"Tag '{tag.Name}' has been deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while deleting the tag. Please try again.";
            Console.WriteLine($"Error deleting tag: {ex.Message}");
        }

        return RedirectToAction(nameof(Index));
    }
}