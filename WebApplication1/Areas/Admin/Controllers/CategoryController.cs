using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Areas.Admin.ViewModels.Category;
using WebApplication1.Interfaces;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateCategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            await _categoryService.CreateCategoryAsync(viewModel);
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

        var category = await _categoryService.GetCategoryByIdAsync(id.Value);
        if (category == null)
        {
            return NotFound();
        }

        return View(new EditCategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditCategoryViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _categoryService.UpdateCategoryAsync(viewModel);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
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

        var category = await _categoryService.GetCategoryViewModelByIdAsync(id.Value);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _categoryService.DeleteCategoryAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Index));
    }
}