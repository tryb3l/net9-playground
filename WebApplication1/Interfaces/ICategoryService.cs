using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Areas.Admin.ViewModels.Category;
using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryViewModel>?> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<List<SelectListItem>> GetAvailableCategoriesAsync();
    Task<CategoryViewModel?> GetCategoryViewModelByIdAsync(int id);
    Task<ServiceResult> CreateCategoryAsync(CreateCategoryViewModel viewModel);
    Task UpdateCategoryAsync(EditCategoryViewModel viewModel);
    Task DeleteCategoryAsync(int id);
    Task<bool> CategoryExistingAsync(int id);
}
