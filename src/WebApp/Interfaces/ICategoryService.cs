using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels.Category;

namespace WebApp.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryViewModel>?> GetAllCategoriesAsync();
    Task<List<SelectListItem>> GetAvailableCategoriesAsync();
    Task<CategoryViewModel?> GetCategoryViewModelByIdAsync(int id);
    Task CreateCategoryAsync(CreateCategoryViewModel viewModel);
    Task UpdateCategoryAsync(EditCategoryViewModel viewModel);
    Task DeleteCategoryAsync(int id);
}
