using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Areas.Admin.ViewModels.Tag;
using WebApplication1.Interfaces;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TagsController : Controller
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
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
            await _tagService.CreateTagAsync(new Models.Tag { Name = viewModel.Name });
            return RedirectToAction(nameof(Index));
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

        if (ModelState.IsValid)
        {
            try
            {
                var tag = await _tagService.GetTagByIdAsync(id);
                if (tag == null)
                {
                    return NotFound();
                }

                tag.Name = viewModel.Name;
                await _tagService.UpdateTagAsync(tag);
            }
            catch (Exception)
            {
                if (!await _tagService.TagExistsAsync(viewModel.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
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
        return View(tag);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _tagService.DeleteTagAsync(id);
        return RedirectToAction(nameof(Index));
    }
}