using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Areas.Admin.ViewModels.Category;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Utils;

namespace WebApp.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    private static class CacheKeys
    {
        public static string AllCategories => "AllCategories";
        public static string AvailableCategoriesSelectList => "AvailableCategoriesSelectList";
        public static string CategoryById(int id) => $"Category-{id}";
    }

    public CategoryService(ICategoryRepository categoryRepository, IMapper mapper, IMemoryCache cache)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<IEnumerable<CategoryViewModel>?> GetAllCategoriesAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeys.AllCategories, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var categories = await _categoryRepository.GetAllAsync();
            return _mapper.Map<List<CategoryViewModel>>(categories);
        });
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _cache.GetOrCreateAsync(CacheKeys.CategoryById(id), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await _categoryRepository.GetByIdAsync(id);
        });
    }

    public async Task<CategoryViewModel?> GetCategoryViewModelByIdAsync(int id)
    {
        var category = await GetCategoryByIdAsync(id);
        if (category == null)
        {
            return null;
        }
        return new CategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            PostCount = category.Posts.Count
        };
    }

    public async Task UpdateCategoryAsync(EditCategoryViewModel model)
    {
        var category = await _categoryRepository.GetByIdAsync(model.Id);
        if (category == null)
        {
            throw new KeyNotFoundException($"Category with ID {model.Id} not found");
        }

        var originalName = category.Name;
        category.Name = model.Name;
        category.Description = model.Description;

        if (originalName != model.Name)
        {
            category.Slug = SlugHelper.GenerateSlug(model.Name);
        }

        await _categoryRepository.UpdateAsync(category);
        await _categoryRepository.SaveChangesAsync();

        InvalidateCategoryCache(model.Id);
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

        InvalidateCategoryCache(id);
    }

    public async Task<bool> CategoryExistingAsync(int id)
    {
        return await GetCategoryByIdAsync(id) != null;
    }

    public async Task<ServiceResult> CreateCategoryAsync(CreateCategoryViewModel viewModel)
    {
        try
        {
            var category = new Category
            {
                Name = viewModel.Name,
                Description = viewModel.Description,
                Slug = SlugHelper.GenerateSlug(viewModel.Name)
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            _cache.Remove(CacheKeys.AllCategories);
            _cache.Remove(CacheKeys.AvailableCategoriesSelectList);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"Error creating category: {ex.Message}");
        }
    }

    public async Task<List<SelectListItem>> GetAvailableCategoriesAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeys.AvailableCategoriesSelectList, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var categories = await _categoryRepository.GetAllAsync();
            return categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
        }) ?? [];
    }

    private void InvalidateCategoryCache(int id)
    {
        _cache.Remove(CacheKeys.CategoryById(id));
        _cache.Remove(CacheKeys.AllCategories);
        _cache.Remove(CacheKeys.AvailableCategoriesSelectList);
    }
}