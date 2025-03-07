using WebApplication1.Areas.Admin.ViewModels.Categories;
using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryViewModel>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<CategoryViewModel?> GetCategoryViewModelByIdAsync(int id);
    Task CreateCategoryAsync(CreateCategoryViewModel viewModel);
    Task UpdateCategoryAsync(EditCategoryViewModel viewModel);
    Task DeleteCategoryAsync(int id);
    Task<bool> CategoryExistingAsync(int id);
}
