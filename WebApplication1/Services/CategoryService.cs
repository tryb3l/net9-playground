using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Areas.Admin.ViewModels.Category;
using WebApplication1.Interfaces;
using WebApplication1.Models;
using WebApplication1.Utils;

namespace WebApplication1.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<CategoryViewModel>> GetAllCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return categories.Select(c => new CategoryViewModel
        {
            Id = c.Id,
            Name = c.Name,
            PostCount = c.Posts?.Count ?? 0
        });
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _categoryRepository.GetByIdAsync(id);
    }

    public async Task<CategoryViewModel?> GetCategoryViewModelByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            return null;
        }
        return new CategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            PostCount = category.Posts?.Count ?? 0
        };
    }

    public async Task UpdateCategoryAsync(EditCategoryViewModel model)
    {
        var category = await _categoryRepository.GetByIdAsync(model.Id);
        if (category == null)
        {
            throw new KeyNotFoundException($"Category with ID {model.Id} not found");
        }
        category.Name = model.Name;
        category.Description = model.Description;

        if (category.Name != model.Name)
        {
            category.Slug = SlugHelper.GenerateSlug(model.Name);
        }

        await _categoryRepository.UpdateAsync(category);
        await _categoryRepository.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new KeyNotFoundException($"Category with ID {id} not found");
        }

        await _categoryRepository.DeleteAsync(category);
        await _categoryRepository.SaveChangesAsync();
    }

    public async Task<bool> CategoryExistingAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        return category != null;
    }

    public async Task CreateCategoryAsync(CreateCategoryViewModel model)
    {
        var category = new Category
        {
            Name = model.Name,
            Description = model.Description,
            Slug = SlugHelper.GenerateSlug(model.Name)
        };

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();
    }

    public async Task<List<SelectListItem>> GetAvailableCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return categories
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
    }
}
