using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels.Category;
using WebApp.Interfaces;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;
    private readonly IActivityLogService _activityLogService;

    public CategoryController(
        ICategoryService categoryService,
        ILogger<CategoryController> logger,
        IActivityLogService activityLogService)
    {
        _categoryService = categoryService;
        _logger = logger;
        _activityLogService = activityLogService;
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
        _logger.LogInformation("Create POST called with Name: {Name}, Description: {Description}",
            viewModel.Name, viewModel.Description);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid");
            foreach (var error in ModelState)
            {
                _logger.LogWarning("Key: {Key}, Errors: {Errors}",
                    error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
            }
            return View(viewModel);
        }

        try
        {
            await _categoryService.CreateCategoryAsync(viewModel);
            _logger.LogInformation("Category created successfully");
            await _activityLogService.LogActivityAsync("Created", "Category", $"Created category: '{viewModel.Name}'");
            TempData["SuccessMessage"] = "Category created successfully.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Category creation failed");
            ModelState.AddModelError("", $"Error creating category: {ex.Message}");
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

        var category = await _categoryService.GetCategoryViewModelByIdAsync(id.Value);
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
                await _activityLogService.LogActivityAsync("Updated", "Category", $"Updated category: '{viewModel.Name}'");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }
        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var category = await _categoryService.GetCategoryViewModelByIdAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }

            await _categoryService.DeleteCategoryAsync(id);
            await _activityLogService.LogActivityAsync("Deleted", "Category",
                $"Deleted category: '{category.Name}' ({category.PostCount} post(s) left uncategorized)");
            TempData["SuccessMessage"] = $"Category '{category.Name}' has been deleted successfully.";
        }
        catch (KeyNotFoundException)
        {
            TempData["ErrorMessage"] = "Category not found.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with ID {CategoryId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the category. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }
}