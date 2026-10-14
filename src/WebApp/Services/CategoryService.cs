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
        public static string AvailableCategoriesSelectList => "AvailableCategoriesSelectList";
    }

    public CategoryService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IEnumerable<CategoryViewModel>?> GetAllCategoriesAsync()
    {
        // Not cached: PostCount depends on live Post state, and CategoryService has no
        // visibility into post writes. Caching this previously left stale counts after every
        // publish or trash action. The query itself is trivial at personal-blog scale.
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
    }

    public async Task<CategoryViewModel?> GetCategoryViewModelByIdAsync(int id)
    {
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
            category.Slug = await EnsureUniqueSlugAsync(SlugHelper.GenerateSlug(model.Name), category.Id);
        }

        await _db.SaveChangesAsync();

        InvalidateCategoryCache();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            throw new KeyNotFoundException($"Category with ID {id} not found");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // FK_Posts_Categories_CategoryId is ON DELETE NO ACTION, so dependent posts have to be
        // detached explicitly or Postgres rejects the delete. Query filters are ignored so
        // trashed posts are detached too. They fall back to "Uncategorized" in the mappings.
        await _db.Posts
            .IgnoreQueryFilters()
            .Where(p => p.CategoryId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CategoryId, (int?)null));

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        InvalidateCategoryCache();
    }

    public async Task CreateCategoryAsync(CreateCategoryViewModel viewModel)
    {
        var category = new Category
        {
            Name = viewModel.Name,
            Description = viewModel.Description,
            Slug = await EnsureUniqueSlugAsync(SlugHelper.GenerateSlug(viewModel.Name))
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        InvalidateCategoryCache();
    }

    /// <summary>
    /// IX_Categories_Slug is unique, so two categories whose names normalise to the same slug
    /// (for example "My Blog" and "My  Blog!") would otherwise collide on save.
    /// </summary>
    private async Task<string> EnsureUniqueSlugAsync(string slug, int? categoryId = null)
    {
        var baseSlug = string.IsNullOrEmpty(slug) ? "category" : slug;

        var candidate = baseSlug;
        var counter = 1;

        while (await _db.Categories
                   .AnyAsync(c => c.Slug == candidate && (!categoryId.HasValue || c.Id != categoryId.Value)))
        {
            var suffix = $"-{counter++}";
            candidate = SlugHelper.Truncate(baseSlug, SlugHelper.MaxSlugLength - suffix.Length) + suffix;
        }

        return candidate;
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

    private void InvalidateCategoryCache()
    {
        _cache.Remove(CacheKeys.AvailableCategoriesSelectList);
    }
}