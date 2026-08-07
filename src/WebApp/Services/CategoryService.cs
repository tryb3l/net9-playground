using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Areas.Admin.ViewModels.Category;
using WebApp.Data;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Utils;

namespace WebApp.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    private static class CacheKeys
    {
        public static string AllCategories => "AllCategories";
        public static string AvailableCategoriesSelectList => "AvailableCategoriesSelectList";
        public static string CategoryById(int id) => $"Category-{id}";
    }

    public CategoryService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IEnumerable<CategoryViewModel>?> GetAllCategoriesAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeys.AllCategories, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await _db.Categories
                .AsNoTracking()
                .Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    PostCount = c.Posts.Count
                })
                .ToListAsync();
        });
    }

    public async Task<CategoryViewModel?> GetCategoryViewModelByIdAsync(int id)
    {
        return await _cache.GetOrCreateAsync(CacheKeys.CategoryById(id), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await _db.Categories
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    PostCount = c.Posts.Count
                })
                .FirstOrDefaultAsync();
        });
    }

    public async Task UpdateCategoryAsync(EditCategoryViewModel model)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == model.Id);
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

        await _db.SaveChangesAsync();

        InvalidateCategoryCache(model.Id);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            throw new KeyNotFoundException($"Category with ID {id} not found");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        InvalidateCategoryCache(id);
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

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

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
            return await _db.Categories
                .AsNoTracking()
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }) ?? [];
    }

    private void InvalidateCategoryCache(int id)
    {
        _cache.Remove(CacheKeys.CategoryById(id));
        _cache.Remove(CacheKeys.AllCategories);
        _cache.Remove(CacheKeys.AvailableCategoriesSelectList);
    }
}