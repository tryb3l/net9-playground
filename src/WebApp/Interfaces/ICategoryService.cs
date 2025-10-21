using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels.Category;
using WebApp.Models;

namespace WebApp.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryViewModel>?> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<List<SelectListItem>> GetAvailableCategoriesAsync();
    Task<CategoryViewModel?> GetCategoryViewModelByIdAsync(int id);
    Task<ServiceResult> CreateCategoryAsync(CreateCategoryViewModel viewModel);
    Task UpdateCategoryAsync(EditCategoryViewModel viewModel);
    Task DeleteCategoryAsync(int id);
}
